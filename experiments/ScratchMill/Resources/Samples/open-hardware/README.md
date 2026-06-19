# Open Hardware ECAD Samples

This folder is a small research set for Gojo ECAD import experiments. The goal is
not to mirror every source file one-to-one. The goal is to study small breakout
boards with enough schematic, layout, documentation, datasheet, and design
intent evidence to build conceptual Gojo ladders.

## Downloaded Samples

| Folder | Board | ECAD format | Why it is useful |
| --- | --- | --- | --- |
| `sparkfun-qwiic-bme280` | SparkFun Qwiic Atmospheric Sensor BME280 | Eagle | Common I2C sensor breakout with Qwiic connector, environmental sensing intent, pullups, power, and simple board geometry. |
| `adafruit-mcp9808` | Adafruit MCP9808 temperature sensor breakout | Eagle | Good address-selection example: one chip, address pins, pull-down/pull-up support circuitry, alert pin, and STEMMA QT variant. |
| `adafruit-bq24074` | Adafruit BQ24074 USB/DC/Solar LiPo charger | Eagle | Charger/power-path board with jumpers, input limits, battery/system rails, and documentation-heavy design intent. |
| `ultra-compact-pmic-bq24074-kicad` | Ultra-compact BQ24074 PMIC board | KiCad | Native KiCad sample with schematic, PCB, production outputs, BOM-style files, and explicit layout/thermal/power rationale. |
| `dual-sensor-breakout-kicad` | BME280 + MPU6050 dual sensor board | KiCad | Very small KiCad sensor board with shared I2C bus, address configuration, regulator, decoupling, and Gerbers. |

## Candidate Matrix

| Priority | Project | Main ICs | Source/doc status | Gojo value |
| --- | --- | --- | --- | --- |
| High | SparkFun Qwiic BME280 | Bosch BME280 | Downloaded; Eagle schematic/board; guide and libraries online | Minimal sensor ladder: product goal -> I2C sensor -> pullups -> connector -> placement/clearance. |
| High | Adafruit MCP9808 | Microchip MCP9808 | Downloaded; Eagle schematic/board; Adafruit guide and datasheet online | Address-selection ladder, alert output, reusable I2C/power basis, multiple board revisions. |
| High | Adafruit BQ24074 charger | TI BQ24074 | Downloaded; Eagle schematic/board; guide, schematic image, fab print, datasheet online | Power-path and jumper concept ladders; excellent for defaults, constraints, and user-configured settlement. |
| High | Ultra-compact PMIC BQ24074 | TI BQ24074 | Downloaded; KiCad schematic/PCB plus production files | Native KiCad parser sample; strong explicit design methodology: thermal, current width, edge connector, isolation, DRC. |
| Medium | Dual sensor breakout | Bosch BME280, MPU6050 | Downloaded; KiCad PCB/project and design notes; no schematic file found in the archive | Useful parser sample, but has less schematic evidence than ideal. Treat missing schematic as import residue. |
| Medium | Adafruit SHT31-D | Sensirion SHT31-DIS | Not downloaded; Eagle source and Adafruit guide available | Humidity/temperature with PTFE filter, heater behavior, address options, and STEMMA/Qwiic connector reuse. |
| Medium | SparkFun Qwiic 9DoF IMU | TDK ICM-20948 | Not downloaded; Eagle source, docs folder, datasheets, guide, library | Rich sensor capability ladder: accel/gyro/mag/DMP, level shifting, GPIO breakout, orientation semantics. |
| Medium | Adafruit MAX17048 fuel gauge | Analog MAX17048 | Not downloaded; Eagle source, guide, 3D model, datasheet | Battery-state concept ladder: cell voltage -> model gauge -> SOC estimate -> alert tension. |
| Medium | SparkFun LiPo Fuel Gauge | MAX17043/MAX17048 family | Not downloaded; Eagle source, firmware, docs, guide | Good board/code pair for ECAD plus firmware behavior. |
| Wildcard | mikoto nRF52840 | Nordic nRF52840, charger/power support | Not downloaded; KiCad 6 files, PDFs, bootloader/software notes | Strong wireless/power/antenna/manufacturing rationale; larger than a simple breakout but valuable for concept ladders. |

## Datasheet Links

- BME280: https://www.bosch-sensortec.com/media/boschsensortec/downloads/datasheets/bst-bme280-ds002.pdf
- MCP9808: https://ww1.microchip.com/downloads/en/DeviceDoc/MCP9808-0.5C-Maximum-Accuracy-Digital-Temperature-Sensor-Data-Sheet-DS20005095B.pdf
- BQ24074: https://www.ti.com/lit/ds/symlink/bq24074.pdf
- SHT31-DIS: https://sensirion.com/media/documents/213E6A3B/63A5A569/Datasheet_SHT3x_DIS.pdf
- ICM-20948: https://product.tdk.com/system/files/dam/doc/product/sensor/mortion-inertial/imu/data_sheet/ds-000189-icm-20948-v1.5.pdf
- MAX17048/MAX17049: https://www.analog.com/media/en/technical-documentation/data-sheets/max17048-max17049.pdf
- nRF52840: https://docs.nordicsemi.com/bundle/ps_nrf52840/page/keyfeatures_html5.html

## Gojo Import Notes

- Treat downloaded ECAD files as source witnesses, not as canonical meaning.
- Preserve missing schematic/source evidence as residue. For example, `dual-sensor-breakout-kicad` has a KiCad PCB but no `.kicad_sch` in the downloaded archive.
- Reusable basis candidates: I2C bus, Qwiic/STEMMA connector, 3.3 V rail, pull-up basis, decoupling basis, address-select basis, battery-cell basis, charge-current basis, trace-width/current basis, layer/side orientation basis.
- Concept ladders to extract first: environmental reading, address selection, power path, battery state, connector ecosystem, sensor orientation, manufacturing settlement.
- Good tension examples: I2C address conflicts, missing schematic evidence, unsupported Eagle files, unfilled KiCad source links, charger current-selection jumpers, thermal/current-width assumptions, antenna/stackup assumptions.
