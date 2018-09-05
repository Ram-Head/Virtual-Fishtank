
/**
 * --------------------------------------------------------------------------------------------------------------------
 * Example sketch/program showing how to read data from more than one PICC to serial.
 * --------------------------------------------------------------------------------------------------------------------
 * This is a MFRC522 library example; for further details and other examples see: https://github.com/miguelbalboa/rfid
 *
 * Example sketch/program showing how to read data from more than one PICC (that is: a RFID Tag or Card) using a
 * MFRC522 based RFID Reader on the Arduino SPI interface.
 *
 * Warning: This may not work! Multiple devices at one SPI are difficult and cause many trouble!! Engineering skill
 *          and knowledge are required!
 *
 * @license Released into the public domain.
 *
 * Typical pin layout used:
 * -----------------------------------------------------------------------------------------
 *             MFRC522      Arduino       Arduino   Arduino    Arduino          Arduino
 *             Reader/PCD   Uno/101       Mega      Nano v3    Leonardo/Micro   Pro Micro
 * Signal      Pin          Pin           Pin       Pin        Pin              Pin
 * -----------------------------------------------------------------------------------------
 * RST/Reset   RST          9             5         D9         RESET/ICSP-5     RST
 * SPI SS 1    SDA(SS)      ** custom, take a unused pin, only HIGH/LOW required **
 * SPI SS 2    SDA(SS)      ** custom, take a unused pin, only HIGH/LOW required **
 * SPI MOSI    MOSI         11 / ICSP-4   51        D11        ICSP-4           16
 * SPI MISO    MISO         12 / ICSP-1   50        D12        ICSP-1           14
 * SPI SCK     SCK          13 / ICSP-3   52        D13        ICSP-3           15
 *
 */



/*constexpr uint8_t RST_PIN = 5;     // Configurable, see typical pin layout above
constexpr uint8_t SS_1_PIN = 53;   // Configurable, take a unused pin, only HIGH/LOW required, must be diffrent to SS 2
constexpr uint8_t SS_2_PIN = 52;    // Configurable, take a unused pin, only HIGH/LOW required, must be diffrent to SS 1
constexpr uint8_t SS_3_PIN = 51;   // Configurable, take a unused pin, only HIGH/LOW required, must be diffrent to SS 2
constexpr uint8_t SS_4_PIN = 50;
constexpr uint8_t SS_5_PIN = 49;
constexpr uint8_t SS_6_PIN = 48;
constexpr uint8_t SS_7_PIN = 47;
constexpr uint8_t SS_8_PIN = 46;

constexpr uint8_t NR_OF_READERS = 1;

byte ssPins[] = {SS_1_PIN, SS_2_PIN, SS_3_PIN, SS_4_PIN, SS_5_PIN, SS_6_PIN, SS_7_PIN, SS_8_PIN};*/
char *corners[] = { "A", "B", "C", "D"};
char *currTag[] =  {"X", "X", "X", "X"};

void initializeReaders(){ 
  #ifdef DEBUG
  Serial.println("Initializing Readers...");
  #endif
  
  for (uint8_t reader = 0; reader < NUMREADERS; reader++) {
    #ifdef DEBUG
    Serial.print(F("Initializing Reader "));
    Serial.print(reader);    
    Serial.print(F(": "));
    #endif
    mfrc522[reader].PCD_Init(ssPins[reader], RST_PIN); // Init each MFRC522 card
    activeReaders[reader] = -1;
    controls[reader] = 0x00;

    mfrc522[reader].PCD_DumpVersionToSerial();
  }
  
  sendNotification("Box("+ BOXNUMBER + ") is Done Initializing RFID Readers!");  
}

uint8_t uidB[] = {0x04, 0x72, 0x8A, 0x72};
uint8_t uidA[] = {0x04, 0x3B, 0x7C, 0x72};
uint8_t uidC[] = {0x04, 0xA7, 0x90, 0x72};
uint8_t uidD[] = {0x81, 0x80, 0x1B, 0x1E};
/**
 * Helper routine to dump a byte array as hex values to Serial.
 */
char* getTagId(byte *buffer, byte bufferSize) {
  bool isA = true;
  for (byte i = 0; i < 4; i++) {
    if (uidA[i] != buffer[i]) {isA = false;}
  }
  if (isA) {return "A";}
  
  bool isB = true;
  for (byte i = 0; i < 4; i++) {
    if (uidB[i] != buffer[i]) {isB = false;}
  }
  if (isB) {return "B";}
  
  bool isC = true;
  for (byte i = 0; i < 4; i++) {
    if (uidC[i] != buffer[i]) {
      Serial.println(i);
      isC = false;
    }
  }
  if (isC) {
    return "C";}
  
  bool isD = true;  
  for (byte i = 0; i < 4; i++) {
    if (uidD[i] != buffer[i]) {isD = false;}
  }
  if (isD) {return "D";}

  return "X";
}


/**
 * Helper routine to dump a byte array as hex values to Serial.
 */
void dump_byte_array(byte *buffer, byte bufferSize) {
  for (byte i = 0; i < bufferSize; i++) {
    Serial.print(buffer[i] < 0x10 ? " 0" : " ");
    Serial.print(buffer[i], HEX);
  }
}

void finishfunct(uint8_t stag, uint8_t reader){
  //MFRC522 mfrc522 = mfrc522[reader];
  Serial.print("Card Removed from Reader ");
  Serial.println(reader);

  if (currTag[reader]!= "X") {
    sendRFIDData(false, corners[reader], currTag[reader]); 
    currTag[reader] = "X";
  }

  mfrc522[reader].PICC_HaltA();
  mfrc522[reader].PCD_StopCrypto1();  
}

void updateReaders(){
  for (uint8_t reader = 0; reader < NUMREADERS; reader++) {
    if (activeReaders[reader] == -1) { //Check if any readers not touch a tag detect a new tag
      if (mfrc522[reader].PICC_IsNewCardPresent() && mfrc522[reader].PICC_ReadCardSerial()) {
        activeReaders[reader] = 0;
        Serial.print(F("Card Placed on Reader "));
        Serial.print(reader);
        // Show some details of the PICC (that is: the tag/card)
        Serial.print(F(" with UID:"));
        dump_byte_array(mfrc522[reader].uid.uidByte, mfrc522[reader].uid.size);
        Serial.println();
        controls[reader] = 0;
        char* tagId = getTagId(mfrc522[reader].uid.uidByte, mfrc522[reader].uid.size);
        if (tagId != "X") {
          currTag[reader] = tagId;
          sendRFIDData(true, corners[reader], tagId); 
        }
      } //if (mfrc522[reader].PICC_IsNewC
    } //for(uint8_t reader
  }

  for (uint8_t reader = 0; reader < NUMREADERS; reader++) {
    if (activeReaders[reader] != -1) { //Check if any readers have their tag removed
      if (activeReaders[reader] < 3) {
        if(!mfrc522[reader].PICC_IsNewCardPresent()){
          //Serial.println("A");
          controls[reader] += 0x1;
        }
        
        controls[reader] += 0x4;
        activeReaders[reader]++;
        //Serial.println("B: " + String(controls[reader]));
      }
      else {
        //Serial.println("C: " + String(controls[reader]));
        activeReaders[reader] = 0;
        if(controls[reader] == 13 || controls[reader] == 14){
          //Serial.println("D: "+ String(controls[reader]));
          controls[reader] = 0;
        } else {
          //Serial.println("E: " + String(controls[reader]));
          activeReaders[reader] = -1;       
          finishfunct(controls[reader], reader);
          controls[reader] = 0;
        }
      }
    }
  }  
}


/*void readTag(MFRC522 mfrc522){
  Serial.print(F(" with UID:"));
  dump_byte_array(mfrc522[reader].uid.uidByte, mfrc522[reader].uid.size);
  
  MFRC522::MIFARE_Key key;
  for (byte i = 0; i < 6; i++) key.keyByte[i] = 0xFF;
  
  Serial.print(F("Data: "));
  byte buffer1[18];
  byte block = 4;
  byte len = 18;
  //------------------------------------------- Corner
  MFRC522::StatusCode status = mfrc522.PCD_Authenticate(MFRC522::PICC_CMD_MF_AUTH_KEY_A, 4, &key, &(mfrc522.uid)); //line 834 of MFRC522.cpp file
  if (status != MFRC522::STATUS_OK) {
    Serial.print(F("Authentication failed: "));
    Serial.println(mfrc522.GetStatusCodeName(status));
    return;
  }

  status = mfrc522.MIFARE_Read(block, buffer1, &len);
  if (status != MFRC522::STATUS_OK) {
    Serial.print(F("Reading failed: "));
    Serial.println(mfrc522.GetStatusCodeName(status));
    return;
  }

  //PRINT Corner
  for (uint8_t i = 0; i < 16; i++)
  {
    if (buffer1[i] != 32)
    {
      Serial.write(buffer1[i]);
    }
  }
  Serial.print(" ");

 
  //---------------------------------------- Wall
  byte buffer2[18];
  block = 1;

  /*status = mfrc522.PCD_Authenticate(MFRC522::PICC_CMD_MF_AUTH_KEY_A, 1, &key, &(mfrc522.uid)); 
  if (status != MFRC522::STATUS_OK) {
    Serial.print(F("Authentication failed: "));
    Serial.println(mfrc522.GetStatusCodeName(status));
    return;
  }

  status = mfrc522.MIFARE_Read(block, buffer2, &len);
  if (status != MFRC522::STATUS_OK) {
    Serial.print(F("Reading failed: "));
    Serial.println(mfrc522.GetStatusCodeName(status));
    return;
  }

  //PRINT Wall
  for (uint8_t i = 0; i < 16; i++) {
    Serial.write(buffer2[i] );
  }*/


  /*//----------------------------------------

  Serial.println(F("\n**End Reading**\n"));

  //delay(50); //change value if you want to read cards faster

  //mfrc522.PICC_HaltA();
  //mfrc522.PCD_StopCrypto1();  
  
}*/
