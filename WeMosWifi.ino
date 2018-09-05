const char* ssid     = "1-10guest tennoz24g";
const char* password = "irZd38sa";
const String url = "http://192.168.100.119:5000/fishtank/dataInput/";
//const char* ssid     = "1-10 tennoz7w24g";
//const char* password = "uhdf82uidujx8";
//const String url = "http://172.16.1.93:5000/fishtank/dataInput/";

const int jsonBufferSize = 400; //calculated using https://arduinojson.org/v5/assistant/


void sendNotification(String contents){
    String contentType = "text/plain";
    sendOverWifi(contentType, contents);
}

void sendRFIDData(bool connection, char* corner, char* tagId){
  String contentType = "application/json";
  String jsonStr = generateRFIDJson(connection, corner, tagId);
  sendOverWifi(contentType, jsonStr);
}

void sendOverWifi(String contentType, String contents){
  //Serial.println("***" + contents);
  if (WiFi.status() == WL_CONNECTED) { //Check WiFi connection status 
    HTTPClient http;  //Declare an object of class HTTPClient
    http.begin(url); //Specify request destination
    delay(250);

    http.addHeader("Content-Type", contentType);
    int httpCode = http.POST(contents); //Send the request
    //int httpCode = http.GET();
    if (httpCode > 0) { //Check the returning code
      #ifdef DEBUG
      String payload = http.getString();   //Get the request response payload
      Serial.println(payload);             //Print the response payload
      #endif
    }else {
      #ifdef DEBUG
      Serial.print("An error ocurred with error code: ");
      Serial.println(httpCode);
      #endif
    }
    http.end();   //Close connection
  }
  else {
    Serial.println("disconnected from wifi");  
  }
  delay(100);
}
//Note that if you change the contents of the json object then you must also change the buffer size
String generateRFIDJson(bool connection, char* corner, char* tagId){
  StaticJsonBuffer<jsonBufferSize> jsonBuffer;
  JsonObject& root = jsonBuffer.createObject();
  root["type"] = "connection";
  root["connect"] = connection;
  JsonObject& info = root.createNestedObject("info");
  info.set("timeStamp", -1);
  JsonObject& info_firstBox = info.createNestedObject("firstBox");
  info_firstBox["boxNo"] = BOXNUMBER;
  info_firstBox["wall"] = "bottom";
  info_firstBox["corner"] = corner;
  
  JsonObject& info_secondBox = info.createNestedObject("secondBox");
  info_secondBox["boxNo"] = OTHERNUMBER;
  info_secondBox["wall"] = "top";
  info_secondBox["corner"] = tagId;
  root.printTo(Serial);
  Serial.println();

  String jsonStr;
  root.printTo(jsonStr);
  return jsonStr;
}

void connectToWifi(){
  Serial.print("Connecting to ");
  Serial.println(ssid);

  // Explicitly set the ESP8266 to be a WiFi-client
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    #ifdef DEBUG
    Serial.println("Connecting..");
    #endif
  }

  #ifdef DEBUG
  Serial.println("Connected to WiFi Network");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
  #endif

  sendNotification("Box(" +BOXNUMBER+ ") Successfully Connected to Wifi Network");
  sendNotification("IP address: " + String(WiFi.localIP()));
}
