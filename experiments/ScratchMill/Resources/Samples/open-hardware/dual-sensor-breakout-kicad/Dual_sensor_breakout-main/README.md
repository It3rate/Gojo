# BME280 + MPU6050 Dual Sensor Breakout Board — IoT PCB Design

> A compact dual-sensor breakout board combining the BME280 environmental sensor (temperature, humidity, pressure) and MPU6050 IMU (accelerometer + gyroscope) on a single 2-layer PCB. I2C interface, 3.3V regulated, with proper decoupling and logic-level compatibility — Gerber-ready.

---

## Overview

This project is a **dual-sensor breakout board** combining two of the most widely used sensors in IoT and robotics applications onto a single compact PCB. The **BME280** provides environmental data — temperature, humidity, and barometric pressure — while the **MPU6050** provides 6-axis inertial measurement — 3-axis accelerometer and 3-axis gyroscope.

Both sensors share the I2C bus with separate configurable addresses. The board includes onboard 3.3V regulation, I2C pull-up resistors, and full decoupling — so it connects directly to any microcontroller (Arduino, ESP32, STM32) without additional components.

This breakout demonstrates the ability to design compact sensor hardware for IoT products: proper I2C bus design, mixed-signal layout awareness, and careful decoupling on noise-sensitive sensor circuits.

---

## Specifications

| Parameter          | Value                                    |
|--------------------|------------------------------------------|
| Sensor 1           | BME280 — temperature, humidity, pressure |
| Sensor 2           | MPU6050 — 3-axis accel + 3-axis gyro     |
| Interface          | I2C (shared bus, separate addresses)     |
| BME280 I2C Address | 0x76 (SDO → GND) / 0x77 (SDO → VCC)     |
| MPU6050 I2C Address| 0x68 (AD0 → GND) / 0x69 (AD0 → VCC)     |
| Supply Voltage     | 3.3V – 5V (onboard regulation to 3.3V)  |
| Logic Level        | 3.3V                                     |
| Regulator          | AMS1117-3.3                              |
| PCB Layers         | 2                                        |
| Board Status       | Design complete, DRC passed              |
| Tool               | KiCad                                    |

---

## Design Highlights

### Shared I2C Bus with Address Flexibility
Both sensors share the same SDA/SCL lines. Address select pads (SDO for BME280, AD0 for MPU6050) are exposed as solder jumpers, allowing the I2C address of each sensor to be configured — useful when running multiple boards on the same bus.

### Onboard 3.3V Regulation
The AMS1117-3.3 LDO accepts 3.3V–5V input and regulates down to a clean 3.3V for both sensors. Input and output bulk capacitors ensure stability. Allows the board to be powered directly from a 5V Arduino without any external regulation.

### Proper I2C Pull-Up Resistors
4.7kΩ pull-up resistors on SDA and SCL ensure correct I2C bus voltage levels. Values selected for reliable communication up to 400 kHz (Fast Mode) across the expected trace lengths and capacitance.

### Sensor Decoupling
Each sensor has dedicated 100nF ceramic decoupling capacitors placed as close as possible to the VCC and VDDIO pins — minimising supply noise coupling into the sensitive analog and MEMS measurement circuits.

### Compact Layout
Both ICs, regulation circuitry, decoupling, and pull-ups are placed within a compact footprint suitable for embedding in drone flight controllers, weather stations, wearables, and IoT sensor nodes.

---

## Repository Structure

```
project4-dual-sensor-breakout/
├── schematic/
│   └── dual_sensor_breakout.kicad_sch
├── pcb/
│   └── dual_sensor_breakout.kicad_pcb
├── gerbers/
│   ├── dual_sensor_breakout.GTL
│   ├── dual_sensor_breakout.GBL
│   ├── dual_sensor_breakout.GTO
│   ├── dual_sensor_breakout.GTS
│   ├── dual_sensor_breakout.GBS
│   └── dual_sensor_breakout.DRL
├── bom/
│   └── bom.csv
└── README.md
```

---

## Bill of Materials

| Ref    | Component           | Part Number   | Package    |
|--------|---------------------|---------------|------------|
| U1     | Environmental Sensor| BME280        | LGA-8      |
| U2     | IMU Sensor          | MPU6050       | QFN-24     |
| U3     | 3.3V LDO Regulator  | AMS1117-3.3   | SOT-223    |
| C1–C2  | Bulk Capacitors     | 10µF          | 0805       |
| C3–C6  | Decoupling Caps     | 100nF         | 0402       |
| R1–R2  | I2C Pull-ups        | 4.7kΩ         | 0402       |
| JP1    | BME280 Addr Select  | Solder jumper | —          |
| JP2    | MPU6050 Addr Select | Solder jumper | —          |
| J1     | Pin Header          | 6-pin, 2.54mm | Through-hole |

---

## Pin Header Pinout

| Pin | Signal | Description                  |
|-----|--------|------------------------------|
| 1   | VIN    | 3.3V – 5V power input        |
| 2   | GND    | Ground                       |
| 3   | SDA    | I2C data                     |
| 4   | SCL    | I2C clock                    |
| 5   | INT    | MPU6050 interrupt output     |
| 6   | 3V3    | Regulated 3.3V output        |

---

## Skills Demonstrated

- I2C bus design with multi-device address management
- MEMS sensor layout (BME280, MPU6050) and decoupling strategy
- LDO regulator circuit design
- Solder jumper address select implementation
- Compact mixed-signal PCB layout on 2 layers
- KiCad DRC and Gerber export

---

## Use Cases

- IoT weather stations
- Drone / quadcopter flight controller peripherals
- Wearable motion and environment tracking
- Robot orientation sensing
- Industrial condition monitoring

---

## Project Status

| Stage            | Status       |
|------------------|--------------|
| Schematic        | ✅ Complete  |
| PCB Layout       | ✅ Complete  |
| DRC              | ✅ Passed    |
| Gerbers Exported | ✅ Ready     |
| Fabricated       | ⬜ Not yet   |
| Tested           | ⬜ Not yet   |

---

## About the Designer

Self-taught PCB designer specializing in KiCad — IoT, embedded systems, and industrial control boards.
Available for freelance PCB design work: schematic capture, layout, DRC review, and Gerber preparation.

🔗 GitHub: [Abhi90808]
📧 [kvabhishek908@gmail.com]
