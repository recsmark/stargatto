using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiderealTimes
{
    public double JD2000; //days since J20000
    public float LST; //Local sidereal time

    public SiderealTimes(double jd, double lst) 
    { 
        JD2000 = jd;
        LST = (float)lst;
    }
}

public static class SiderealMath
{
    public static SiderealTimes CalculateLocalSiderealAngle(float longitude, DateTime refUtcDate)
    {
        // 1. Add UTC time to ref date from UI input
        DateTime localTime = DateTime.UtcNow;
        DateTime utcTime = refUtcDate.Date + localTime.TimeOfDay;

        // 2. Calc days from J2000 Epoch
        double daysSinceJ2000 = GetDaysSinceJ2000(utcTime);

        // 3. GMST = LST at GreenWich
        double gmst = CalculateGMST(daysSinceJ2000); //[deg]

        // 4. Apply longitude correction
        double lst = (gmst + longitude) / 360 * 24; //local sidereal time

        // 5. Normalize in range 0..24h
        lst = lst % 24.0;
        if (lst < 0)
        {
            lst += 24.0;
        }

        SiderealTimes output = new(daysSinceJ2000, lst);

        return output;
    }

    private static double GetDaysSinceJ2000(DateTime utcNow)
    {
        // Epoch J2000: 1 Jan 2000, ore 12:00:00 UTC
        DateTime j2000 = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        TimeSpan timeDifference = utcNow - j2000;

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
    private float _LST_hour = 0;
    private float _UTC_rotated = 0;
    private double _JDate = 0;
    private float _initialRotationHour = 0;
    private const float skyRotationSpeed = 15f; //[deg/hour]
    //Lat/Lon of Costigiola https://costigiola.it/dove-siamo/
    private const float initLatitude = 45.6f;  //[deg]
    private const float initLongitude = 14.6f; //[deg]

    private float currLatitude = initLatitude;
    private float currLongitude = initLongitude;

    public DateValidator dateValidator;
    public LocationValidator locationValidator;

    private bool rotationEnabled = true;

    // Example of complete setter-getter structure
    public float SkyRotationLST
    {
        get
        {
            return _LST_hour;
        }
    }

    public float SkyRotationUTC
    {
        get
        {
            return _UTC_rotated;
        }
    }

    public double SkyJDate
    {
        get
        {
            return _JDate;
        }
    }

    public void Initialize()
    {
        DateTime initDate = DateTime.UtcNow;
        //Apply(initDate); will be called when lat/long will be initialized
        if (dateValidator != null)
            dateValidator.OnDateSuccessfullyValidated += SetNewDate;

        if (locationValidator != null)
            locationValidator.OnLocSuccessfullyValidated += SetNewDate;
    }

    public void EnableRotationFromProjection(bool alternativeProj)
    {
        rotationEnabled = !alternativeProj;
        SetNewDate();
        _LST_hour = _initialRotationHour; //override
        _UTC_rotated = 0; //TBD ?
    }

    public void SetNewDate()
    {
        currLatitude = locationValidator.currentLatitude; 
        currLongitude = locationValidator.currentLongitude;

        DateTime dateRef = dateValidator?.GetDate() ?? DateTime.UtcNow;
        DateTime utcTime = DateTime.UtcNow;
        DateTime utc = dateRef.Date + utcTime.TimeOfDay; //mix date from validator with current time
        //transform.Rotate(Vector3.forward, -_deltaRotationHour);
        /*
        transform.eulerAngles = new Vector3(0f, 0f, 0f);
        _LST_hour = 0; //set again to 0 to proper init
        _UTC_rotated = 0;
        */
        Apply(utc, true);
    }

    private void Apply(DateTime dateRef, bool absolute)
    {
        // The sign of LST is inverted for the Southern Hemisphere to account for the mirrored stereographic projection.
        SiderealTimes output = SiderealMath.CalculateLocalSiderealAngle(currLongitude, dateRef);
        _initialRotationHour = Mathf.Sign(currLatitude) * output.LST;
        _JDate = output.JD2000;
        _UTC_rotated -= _initialRotationHour; //instantiate difference, assuming the same speed

        //modifier correction
        // Adds a 90 deg offset
        // This aligns the Cartesian mathematical zero (X-axis) with the astrolabe's visual local meridian (Y-axis).
        // e.g. 0 deg is on the right side on the Cartesian plane, but astronomical zero is Noon (upper side)!
        // The sign is inverted for the Southern Hemisphere to account for the mirrored stereographic projection.
        float modifier = (Mathf.Sign(currLatitude) * 90f) / skyRotationSpeed;

        ApplyRotation(_initialRotationHour + modifier, absolute);

        initialSkyRotation = transform.rotation;
    }

    public void ApplyRotation(float deltaTime, bool absolute, float speed = skyRotationSpeed)
    {
        if (!rotationEnabled)
        {
            transform.localRotation = Quaternion.identity;
            return; //azimuthal projection can't rotate around polaris
        }

        //float deltaAngle = deltaTime * speed;
        if (absolute) //reset scene user input
        {
            //here speed must be the constant of conversion 15 [deg/h]
            transform.rotation = Quaternion.Euler(0f, 0f, (skyRotationSpeed * deltaTime));
            _LST_hour = deltaTime;
            _LST_hour %= 24f;
            _UTC_rotated = deltaTime;
            _UTC_rotated %= 24f;
        }
        else //with user input
        {
            transform.Rotate(Vector3.forward, speed * deltaTime);
            _LST_hour += deltaTime;
            _LST_hour %= 24f;
            _UTC_rotated += deltaTime;
            _UTC_rotated %= 24f;
        }
    }

    public void ApplyReset()
    {
        SetNewDate(); //revisited reset
    }
}
