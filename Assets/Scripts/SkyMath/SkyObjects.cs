using System;
using UnityEngine;
using static UniversalConst;

public static class UniversalConst
{
    public const double Deg2Rad = Math.PI / 180.0;
    public const double EarthObliq = 23.43928; // [deg]
    public const double J2000 = 2451545.0; // [days]
    public static readonly double cEarthObl = Math.Cos(EarthObliq * Deg2Rad);
    public static readonly double sEarthObl = Math.Sin(EarthObliq * Deg2Rad);

    public static double GetJ2000Cy(double JDate)
    {
        //JDate already are days from J2000
        return (JDate / 36525f); //centuries from J2000
        //if not, use return (JDate - J2000) / 36525f;
    }
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

    public double Update(double T = 0) //centuries
    {
        return paramJ2000 + param_dot * T;
    }
}

//[Serializable]
public abstract class SkyObject
{
    public uint id;
    public string name;
    public string engName;
    public float absMag;
    public float mag;
    public float colorIndex;
    public Vector3 pos = new (0f, 0f, 0f);  //Cartesian position "now" [variable]
    public float ra;  //[h]
    public float dec; //[deg]
#if USE_DB_ANGULAR_VALUES
    public float dist; // [variable]
    public void updatePos(double x = 0) { } //placeholder
#else //else, calc angular values from x,y,z: better to take into account speed over time
    public float dist { get => pos.magnitude; } // [variable]
    public virtual void UpdatePos(double JDate = J2000)  //input argument needed for the overrided methods
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
    public bool mainStar; //main Star in a binary system
    public uint binMain;
    public byte binInd;
    public uint[] idsBins = new uint[2] { 0, 0 };
    public byte binCount = 0;

    public Vector3 posJ2000 = new(0f, 0f, 0f); //Cartesian position @J2000 [pc]

    public Vector3 vel = new(0f, 0f, 0f); //Cartesian velocity [pc / cy]

#if !USE_DB_ANGULAR_VALUES
    public override void UpdatePos(double JDate = J2000)
    {
        double T = GetJ2000Cy(JDate);
        pos = posJ2000 + vel * (float)T; //[pc]
        base.UpdatePos();
    }
#endif
}

//[Serializable]
public class Planet : SkyObject
{
    // DATA from JPL
    //https://ssd.jpl.nasa.gov/planets/approx_pos.html
    public OrbitalParam aJ2000 { get; } //major semiaxis [UA] // no need to convert in [pc], RA and DEC will be obtained
    public OrbitalParam eJ2000 { get; } //eccentricity [rad]
    public OrbitalParam iJ2000 { get; } //orbit inclination [deg]
    public OrbitalParam LJ2000 { get; } //mean longitude (-> mean anomaly) [deg]
    public OrbitalParam wJ2000 { get; } //perihelion longitude (-> Argument of periapsis) [deg]
    public OrbitalParam ΩJ2000 { get; } //longitude of ascending node (RAAN) [deg]

    //additional orbital terms (Jupiter to Neptune) based on centuries from J2000
    public float addB { get; set; } = 0; //base T^2 
    public float addC { get; set; } = 0; //cosine term
    public float addS { get; set; } = 0; //sine term
    public float addF { get; set; } = 0; //freq term

    public string descr;

    public Planet(double a, double e, double i, double L, double w, double Ω)
    {
        aJ2000 = new OrbitalParam(a, 0f);
        eJ2000 = new OrbitalParam(e, 0f); 
        iJ2000 = new OrbitalParam(i, 0f); 
        LJ2000 = new OrbitalParam(L, 0f); 
        wJ2000 = new OrbitalParam(w, 0f); 
        ΩJ2000 = new OrbitalParam(Ω, 0f);
    }

    public void SetOrbitalParamDot(double aV, double eV, double iV, double LV, double wV, double ΩV)
    {
        aJ2000.param_dot = aV;
        eJ2000.param_dot = eV;
        iJ2000.param_dot = iV;
        LJ2000.param_dot = LV;
        wJ2000.param_dot = wV;
        ΩJ2000.param_dot = ΩV;
    }

    public void SetAdditionalTerms(float b, float c, float s, float f)
    {
        addB = b;
        addC = c;
        addS = s;
        addF = f;
    }

#if !USE_DB_ANGULAR_VALUES
    public void UpdatePos(Vector3 earthPos, double JDate = J2000)
    {
        double CalcEccAnomaly (double M, double e) //Eccentric (real) anomaly 
        {
            const double tol = 10e-6; // 10^(-6)
            uint counter = 0;
            double eStar = 57.29578 * e; // (180/pi)*e
            double deltaM, deltaE;
            double E = M + eStar * Math.Sin(M *Deg2Rad); //E0 from JPL page
            while (counter < 10) //Solve Kepler's Equation iterativelly
            {
                deltaM = M - (E - eStar * Math.Sin(E * Deg2Rad));
                deltaE = deltaM/(1.0 - e * Math.Cos(E  * Deg2Rad));
                if (Math.Abs(deltaE) < tol)
                    break;
                E += deltaE;
                ++counter;
            }
            return E;
        }

        double T = GetJ2000Cy(JDate);
        float Trad = (float)(Deg2Rad * T);

        double a = aJ2000.Update(T);
        double e = eJ2000.Update(T);
        double i = iJ2000.Update(T);
        double L = LJ2000.Update(T);
        double w = wJ2000.Update(T);
        double Ω = ΩJ2000.Update(T);

        double wp = w - Ω; //argument of perihelium
        double M = L - w + (addB * T * T) 
                         + (addC * Mathf.Cos(addF * Trad)) 
                         + (addS * Mathf.Sin(addF * Trad)); //mean anomaly

        //normalization mean anomaly in [-180 .. 180]
        M = ((M + 180.0) % 360.0 + 360.0) % 360.0 - 180.0;

        double E = CalcEccAnomaly(M, e);

        //heliocentric coordinates
        double xH = a * (Math.Cos(E * Deg2Rad) - e);
        double yH = a * Math.Sqrt(1 - (e*e)) * Math.Sin(E * Deg2Rad);
        //double zH = 0;

        //in the elliptic plane
        double cw = Math.Cos(wp * Deg2Rad); double sw = Math.Sin(wp * Deg2Rad);
        double cΩ = Math.Cos(Ω * Deg2Rad); double sΩ = Math.Sin(Ω * Deg2Rad);
        double ci = Math.Cos(i * Deg2Rad); double si = Math.Sin(i * Deg2Rad);
        double xE = (cw * cΩ - sw * sΩ * ci) * xH + (-sw * cΩ - cw * sΩ * ci) * yH;
        double yE = (cw * sΩ + sw * cΩ * ci) * xH + (-sw * sΩ + cw * cΩ * ci) * yH;
        double zE = (sw * si) * xH + (cw * si) * yH;

        //in the equatorial plane, referenced to Earth
        pos.x = -earthPos.x + (float) xE;
        pos.y = -earthPos.y + (float) (cEarthObl * yE - sEarthObl * zE);
        pos.z = -earthPos.z + (float) (sEarthObl * yE + cEarthObl * zE);

        base.UpdatePos();
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