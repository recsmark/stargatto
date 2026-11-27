using UnityEngine;
using System;
using JetBrains.Annotations;
using UnityEngine.InputSystem.Controls;

using static UniversalConst;

public static class UniversalConst
{
    public const double Deg2Rad = Math.PI / 180.0;
    public const double EarthObliq = 23.43928f; // [deg]
    public static readonly double cEarthObl = Math.Cos(EarthObliq * Deg2Rad);
    public static readonly double sEarthObl = Math.Sin(EarthObliq * Deg2Rad);
}

public enum LinkType
{
    NO_LINK_TYPE = 0,
    CONSTELLATION = 1,
    ASTERISM = 2,
    GEOMETRY = 3
}

//[Serializable]
public class OrbitalParam
{
    public double paramJ2000;
    public double param_dot; //rate per century
    //accuracy not taken into account

    public OrbitalParam(double p, double pDot)
    {
        paramJ2000 = p;
        param_dot = pDot;
    }

    public double Get(float yearFromJ2000 = 0)
    {
        return paramJ2000 + param_dot * yearFromJ2000;
    }
}

//[Serializable]
public abstract class SkyObject
{
    public string name;
    public string engName;
    public float absMag;
    public float mag;
    public Vector3 pos = new (0f, 0f, 0f);  //Cartesian position "now" [variable]
    public float ra;  //[h]
    public float dec; //[deg]
#if USE_DB_ANGULAR_VALUES
    public float dist; // [variable]
    public void updatePos(float x = 0) { } //placeholder
#else //else, calc angular values from x,y,z: better to take into account speed over time
    public float dist { get => pos.magnitude; } // [variable]
    public virtual void UpdatePos(float J2000date = 0)  //input argument needed for the overrided methods
    {
        //recalc ra, dec and dist from new pos
        ra = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg / 15f; //[h]
        if (ra < 0f)
        {
            ra += 24f;
        }

        float xyProj = Mathf.Sqrt(pos.x * pos.x + pos.y * pos.y);
        dec = Mathf.Atan2(pos.z, xyProj) * Mathf.Rad2Deg;
    } //must be overridded specifically in each derived class
#endif
}

//[Serializable]
public class Star : SkyObject
{
    public uint hr;
    public uint id;
    public bool mainStar; //main Star in a binary system
    public float colorIndex;
    public uint binMain;
    public byte binInd;
    public uint[] idsBins = new uint[2] { 0, 0 };
    public byte binCount = 0;

    public Vector3 posJ2000 = new(0f, 0f, 0f); //Cartesian position @J2000 [pc]

    public Vector3 vel = new(0f, 0f, 0f); //Cartesian velocity [pc / cy]

#if !USE_DB_ANGULAR_VALUES
    public override void UpdatePos(float J2000date = 2451545.0f)
    {
        float T = (J2000date - 2451545.0f) / 36525f; //centuries
        pos = posJ2000 + vel * T; //[pc]
        base.UpdatePos(J2000date);
    }
#endif
}

//[Serializable]
public class Planet : SkyObject
{
    //https://ssd.jpl.nasa.gov/planets/approx_pos.html
    private readonly OrbitalParam a0; //major semiaxis
    private readonly OrbitalParam e0; //eccentricity
    private readonly OrbitalParam i0; //orbit inclination
    private readonly OrbitalParam L0; //mean longitude (-> mean anomaly)
    private readonly OrbitalParam w0; //perihelion longitude (-> Argument of periapsis)
    private readonly OrbitalParam Ω0; //longitude of ascending node (RAAN)

    //additional orbital terms (Jupiter to Neptune) based on centuries from J2000
    private float addB { get; set; } = 0; //base T^2 
    private float addC { get; set; } = 0; //cosine term
    private float addS { get; set; } = 0; //sine term
    private float addF { get; set; } = 0; //freq term

    protected Planet(double a, double e, double i, double L, double w, double Ω)
    {
        a0 = new OrbitalParam(a, 0f);
        e0 = new OrbitalParam(e, 0f); 
        i0 = new OrbitalParam(i, 0f); 
        L0 = new OrbitalParam(L, 0f); 
        w0 = new OrbitalParam(w, 0f); 
        Ω0 = new OrbitalParam(Ω, 0f);
    }

    public void setOrbitalParamDot(double aV, double eV, double iV, double LV, double wV, double ΩV)
    {
        a0.param_dot = aV;
        e0.param_dot = eV;
        i0.param_dot = iV;
        L0.param_dot = LV;
        w0.param_dot = wV;
        Ω0.param_dot = ΩV;
    }

    public void setAdditionalTerms(float b, float c, float s, float f)
    {
        this.addB = b;
        this.addC = c;
        this.addS = s;
        this.addF = f;
    }

#if !USE_DB_ANGULAR_VALUES
    public void UpdatePos(Vector3 eathPos, float J2000date = 0)
    {
        double CalcEccAnomaly (double M, double e) //Eccentric (real) anomaly 
        {
            double tol = 10E-6;
            uint counter = 0;
            double E;
            double deltaM, deltaE;
            E = M + e * Math.Sin(M *Deg2Rad); //E0 from JPL
            while (counter < 10) //Solve Kepler's Equation iterativelly
            {
                deltaM = M - (E * e * Math.Sin(E * Deg2Rad));
                deltaE = deltaM/(1 - e * Math.Cos(E  * Deg2Rad));
                if (deltaE < tol)
                    break;
                E = E + deltaE;
            }
            return E;
        }

        float T = (J2000date - 2451545.0f) / 36525; //centuries, TBD

        double a = a0.Get(T);
        double e = e0.Get(T);
        double i = e0.Get(T);
        double L = e0.Get(T);
        double w = e0.Get(T);
        double Ω = e0.Get(T);

        double ω = w - Ω; //argument of perihelium
        double M = L - w + addB * T * T + (addC * Mathf.Cos(addF * T)) + (addS * Mathf.Sin(addF * T)); //mean anomaly
        if (M > 180f) 
            M -= 360f;
        double E = CalcEccAnomaly(M, e);

        //heliocentric coordinates
        double xH = a * Math.Cos(E * Deg2Rad);
        double yH = a * Math.Sqrt(1 - (e*e)) * Math.Sin(E * Deg2Rad);
        //double zH = 0;

        //in the elliptic plane
        double cw = Math.Cos(ω * Deg2Rad); double sw = Math.Sin(ω * Deg2Rad);
        double cΩ = Math.Cos(Ω * Deg2Rad); double sΩ = Math.Sin(Ω * Deg2Rad);
        double ci = Math.Cos(i * Deg2Rad); double si = Math.Sin(i * Deg2Rad);
        double xE = (cw * cΩ - sw * sΩ * ci) * xH + (-sw * cΩ - cw * sΩ * ci) * yH;
        double yE = (cw * sΩ - sw * cΩ * ci) * xH + (-sw * sΩ + cw * cΩ * ci) * yH;
        double zE = (sw * si) * xH + (cw * si) * yH;

        //in the equatorial plane, referenced to Earth
        pos.x = -eathPos.x + (float) xE;
        pos.y = -eathPos.y + (float) (cEarthObl * yE - sEarthObl * zE);
        pos.z = -eathPos.z + (float) (sEarthObl * yE - cEarthObl * zE);

        base.UpdatePos(J2000date);
    }
#endif
}

    public class ConstellationInfo //TBD csv ES
{
    public string id;
    public string itaName;
    public string engName;
    public string meaning;
    public string hemisphere;
    public LinkType type = LinkType.NO_LINK_TYPE;
    public string colorStr = "000000"; //no color
    public Color color = Color.clear; //no color
}

public class StarsLinkItem
{
    public string identifier;
    public uint startId;
    public uint endId;
}