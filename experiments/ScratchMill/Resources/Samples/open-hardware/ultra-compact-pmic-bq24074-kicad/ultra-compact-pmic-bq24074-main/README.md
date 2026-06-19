# ultra-compact-pmic-bq24074

## 1. Project Overview
This repository contains the design files for an ultra-compact (11mm x 30mm) Power Management Unit (PMU) specifically engineered for wearable AR/VR devices. The module is built around the **Texas Instruments BQ24074** PMIC, providing a unified solution for USB Type-C power input, Lithium-Polymer battery charging, and regulated system output (VOUT).

The design prioritizes high power density, stringent thermal management, and IPC-compliant routing within a highly constrained physical footprint.

## 2. Technical Specifications
* **Form Factor:** 11mm x 30mm (Edge-mount optimized)
* **PMIC:** TI BQ24074 (Standalone Li-Ion/Li-Pol Charger with Power Path Management)
* **Input (VBUS):** USB Type-C Receptacle (6-Pin, Power Only, Edge-Mount)
* **Output Interface:** 5-Pin 0.5mm Pitch Horizontal FPC Connector
* **Protection:** ESD and Transient Voltage Suppression (TVS) on VBUS (DFN1006 package)
* **Component Sizing:** Optimized for 0201 (Imperial) passive components to minimize parasitic inductance and footprint.

## 3. Engineering Analysis & Design Methodology

### 3.1 Layout and I/O Integration
* **Mechanical Alignment:** The J1 Type-C connector is positioned with mechanical GND pads flush to the board edge (Edge.Cuts), ensuring structural integrity and precise alignment for wearable frames.
* **Isolation Zones:** Battery termination pads (BATT+/BATT-) are isolated from the main PMIC logic to prevent short circuits during manual soldering/assembly.

### 3.2 Power Integrity and Thermal Strategy
* **High-Current Pathing:** VBUS, VOUT, and BAT tracks are routed with a minimum width of 0.5mm to sustain peak charging currents and minimize IR drop.
* **Decoupling Protocol:** Low-ESR ceramic capacitors (C1, C2) are placed in immediate proximity to the PMIC pins to suppress high-frequency noise and ensure voltage stability.
* **Ground Plane & Via Stitching:** A dual-layer solid ground plane strategy was employed. Isolated GND islands on the top layer are interconnected to the primary bottom layer ground plane through a high-density via stitching matrix. This facilitates both low-impedance return paths and efficient thermal dissipation for the BQ24074.

### 3.3 Verification
* **DRC Compliance:** 0 Design Rule Check (DRC) violations based on 0.1mm minimum clearance constraints.
* **Manufacturing Ready:** All footprints and clearances are verified for standard SMT assembly processes.

## 4. Repository Structure

The production files are organized according to industry-standard fabrication requirements:

1. **`1_Gerber_Files/`** : Fabrication data (.gbr) and drill files (.drl) for PCB manufacturing.
2. **`2_Pick_And_Place/`** : Component centroid data (.pos / .csv) for automated SMT assembly.
3. **`3_BOM_List/`** : Consolidated Bill of Materials including manufacturer part numbers and footprints.
4. **`4_Documentation/`** : High-resolution schematic (PDF) and design specifications.

## 5. Mechanical Integration Note
The current revision (Rev 1.0) focuses on electrical validation and thermal optimization. 3D STEP models for mechanical components (USB-C, FPC, and Tactile Switch) are excluded from this release to allow for custom mechanical alignment with specific frame geometries.

## 6. License
This project is licensed under the **CERN Open Hardware Licence Version 2 - Permissive (CERN-OHL-P)**. See the `LICENSE` file for more details.
---
*Designer:* **Mustafa Ege KILIÇ** *Date:*  08 April 2026  
*Project Version:* Rev 1.0
