//See RFID for info on how to set RFID


#define DEBUG
#include <SPI.h>
#include <MFRC522.h>
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
#include <ArduinoJson.h>

const String BOXNUMBER = "1";
const String OTHERNUMBER = "0";
constexpr uint8_t NUMREADERS = 2;
constexpr uint8_t RST_PIN = D9;     // Configurable, see typical pin layout above
byte ssPins[] = {D10, D8, D7, D6, D5, D4, D3, D2};

MFRC522 mfrc522[NUMREADERS];   // Create MFRC522 instance.
int activeReaders[NUMREADERS];
uint8_t controls[NUMREADERS];

void setup() {
  Serial.begin(9600); // Initialize serial communications with the PC
  //plz make general
  connectToWifi();
  SPI.begin();        // Init SPI bus
  initializeReaders();
  sendNotification("Box("+ BOXNUMBER + ") is Ready to Go!");
}

/**
 * Main loop.
 */
void loop() {
  updateReaders();
}
