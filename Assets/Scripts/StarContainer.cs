using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SiderealMath
{
    /// <summary>
    /// Funzione principale da chiamare. Calcola l'angolo di rotazione del cielo in gradi (0-360).
    /// </summary>
    /// <param name="longitude">La longitudine in gradi (Es. 15f per il meridiano standard GMT+1)</param>
    /// <returns>Angolo in gradi (float) pronto da applicare all'asse di rotazione</returns>
    public static float CalculateLocalSiderealAngle(float longitude, DateTime refUtcDate)
    {
        // 1. Convertiamo in UTC per avere uno standard assoluto rispetto all'Epoca J2000
        DateTime localTime = DateTime.UtcNow;
        DateTime utcTime = refUtcDate.Date + localTime.TimeOfDay;

        // 2. Troviamo i giorni trascorsi
        double daysSinceJ2000 = GetDaysSinceJ2000(utcTime);

        // 3. Calcoliamo il tempo di Greenwich
        double gmst = CalculateGMST(daysSinceJ2000); //[deg]

        // 4. Applichiamo la correzione locale (Est è positivo, Ovest è negativo)
        double lst = (gmst + longitude) / 360 * 24; //local sidereal time

        // 5. Normalize in range 0..24h
        lst = lst % 24.0;
        if (lst < 0)
        {
            lst += 24.0;
        }

        return (float)lst;
    }

    // --- FUNZIONI DI SUPPORTO INTERNE ---

    private static double GetDaysSinceJ2000(DateTime utcNow)
    {
        // Epoca J2000: 1 Gennaio 2000, ore 12:00:00 UTC
        DateTime j2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        TimeSpan timeDifference = utcNow - j2000;

        // Ritorna la differenza totale in giorni, inclusi i decimali per le ore/minuti
        return timeDifference.TotalDays;
    }

    private static double CalculateGMST(double daysSinceJ2000)
    {
        //in C# i can apply % operator between floats
        //z is the result of x % y and is computed as x – n* y, where n is the largest possible integer that is less than or equal to x / y.

        //simplified formula from https://aa.usno.navy.mil/faq/GAST
        double gmstHours = (18.697375 + 24.065709824279 * daysSinceJ2000);
#if EXTREME_PRECISION_CLOCK 
        //the following formulas are not needed for this purpopse, but let's have them implemented for completeness
        //since, as reported, max eqeq value is 1.1 [s]
        const double deg2Rad = Math.PI / 180.0;
        double omega = 125.04 - 0.052954 * daysSinceJ2000; //longitude of ascending node of Moon [deg]
        double L = 280.47 + 0.98565 * daysSinceJ2000; // Mean Longitude of Sun [deg]
        double deltaPsi = -0.000319 * Math.Cos(omega*deg2Rad) - 0.000024 * Math.Sin(2 * L* deg2Rad); //nutation in longitude, [h]
        double epsilon = 23.4393 - 0.0000004 * daysSinceJ2000; //obliquity [deg]
        double eqeq = deltaPsi * Math.Cos(epsilon * deg2Rad);
        gmstHours += eqeq;
#endif
        gmstHours %= 24.0;
        double gmst = (gmstHours * 360.0) / 24.0; //deg

        //AI generated formula ??? it gives 80 deg more than "official" GMST during the test
        //double gmst2 = 280.46061837 + (360.98564736629 * daysSinceJ2000)%360.0; //[deg] 

        while (gmst < 0) //theoretically right but not needed in 2026?
        {
            gmst += 360.0;
        }

        return gmst;
    }
}

public class StarContainer : MonoBehaviour
{
    private Quaternion initialSkyRotation;
    private float _deltaRotationHour = 0;
    private float _initialDeltaRotationHour = 0;
    private float _initialRotationHour = 0;
    private const float skyRotationSpeed = 15f; //[deg/hour]
    //Lat/Lon of Costigiola https://costigiola.it/dove-siamo/
    private const float initLatitude = 45.6f;  //[deg]
    private const float initLongitude = 14.6f; //[deg]
    public float localLatitude => initLatitude;
    public float localLongitude => initLongitude;

    public DateValidator validator;

    // Example of complete setter-getter structure
    public float SkyRotation
    {
        get
        {
            //Prevents date jumps
            if (_deltaRotationHour + _initialDeltaRotationHour >= 24f)
            {
                return _deltaRotationHour - 24f;
            }
            else if (_deltaRotationHour + _initialDeltaRotationHour < 0)
            {
                return _deltaRotationHour + 24f;
            }
            else
            {
                return _deltaRotationHour;
            }
        }
        /*
        set
        {
            //update a variable, then a method can be called
        }
        */
    }

    private void Apply(DateTime dateRef)
    {
        _initialRotationHour = SiderealMath.CalculateLocalSiderealAngle(localLongitude, dateRef);
        ApplyRotation(_initialRotationHour);
        initialSkyRotation = transform.rotation;
    }

    public void Initialize()
    {
        DateTime initDate = DateTime.UtcNow;
        Apply(initDate);
        validator.OnDateSuccessfullyValidated += SetNewDate;
    }

    public void SetNewDate()
    {
        DateTime dateRef = validator?.GetDate() ?? DateTime.UtcNow;
        DateTime utcTime = DateTime.UtcNow;
        DateTime utc = dateRef.Date + utcTime.TimeOfDay; //mix date from validator with current time
        //transform.Rotate(Vector3.forward, -_deltaRotationHour);
        transform.eulerAngles = new Vector3(0f, 0f, 0f);
        _deltaRotationHour = 0; //set again to 0 to proper init
        Apply(utc);
    }

    public void ApplyRotation(float deltaTime, float speed = skyRotationSpeed)
    {
        float deltaAngle = deltaTime * speed;
        transform.Rotate(Vector3.forward, speed * deltaTime);
        _deltaRotationHour += deltaTime;
        _deltaRotationHour %= 24f;
    }

    public void ApplyReset()
    {
        SetNewDate(); //revisited reset
    }
}
