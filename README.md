# [StarGatto]
A Unity-based, WebGL-optimized interactive planetarium/stellar simulation.
A mobile version is planned to be released in the future. under development.
Playable online version with a PC at [this link](https://recsmark.github.io/stargatto/).
Developed as personal project for educational purposes inside [Per Azimut ad Astra](https://costigiola.it/per-azimut-ad-astra/) workshop for AGESCI scout members.

Find constellations and learn about our sky!

## Data Structure
The astronomical data is loaded dynamically via the `StreamingAssets` folder to ensure WebGL compatibility:
* `stars.dat`: A custom compiled binary file containing star coordinates, IDs, and magnitudes and all the needed data for fast parsing.
The .dat file is generated from the following extenal sources

## Data Sources and Acknowledgments
This project relies on the hard work of the astronomical community. The data used to generate the night sky is sourced from:
1. **Stellar Database:** Derived from [HYG Database v4.2 from astromexus.com](https://www.astronexus.com/projects/hyg),
2. **Constellation Lines:** Sourced from [Marc van der Sluys' ConstellationLines](https://github.com/MarcvdSluys/ConstellationLines).

Audio and video assets used in the application UI are copyright-free / Public Domain.
**Menu music**: [Beyond the Gate - Inspired by Stargate]() created by Luis_Humanoide,
**Menu video loop** dowloaded from [Pixabay](https://pixabay.com/videos/space-stars-nebula-galaxy-universe-36471/)
**Game music**: 'Space Ambient' created by [SolarFLEX](https://pixabay.com/it/users/solarflex-54712313/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=569588)

---

## License

This project contains different components that are licensed separately. By using, downloading, or sharing this repository, you must respect the following licenses:

### 1. Main Project & Source Code
The original source code, Unity project files, and application design are licensed under the **Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0)** license.
* You are free to share, copy, and redistribute the material in any medium or format.
* You must give appropriate credit.
* You may **not** use the material for commercial purposes.
* If you remix, transform, or build upon the material, you may **not** distribute the modified material.
*(See the `LICENSE.md` file for full details).*

### 2. Astronomical Data
The datasets included in the `Assets/StreamingAssets` folder are derivative works and strictly retain their original open-data licenses:
* **`db.dat`**: Licensed under **[Creative Commons Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)](https://creativecommons.org/licenses/by-sa/4.0/)**. 
* **`constellation_ilines.csv`**: Licensed under **[Creative Commons Attribution 4.0 International (CC BY 4.0)](https://creativecommons.org/licenses/by/4.0/)**.