/* Hardware connections:
  +5V        VDD               Power supply
  GND        GND               Ground
  5          OUT               Capacitive touch state output
*/

// Constants
const int TOUCH_PIN = 5;    // Input pin for touch state

// Global Variables
int touchState = 0;        // to read touch
int n_licks = 0;            // to count licks/touches
int lick_flag = 0;
char frame_state = 'n';        // to read Unity input
unsigned long previousMillis = 0;   // keep time
const long interval = 5;            // don't count tiny changes

// Functions
void setup() {
  Serial.begin(57600);

  // Configure touch pin as input
  pinMode(TOUCH_PIN, INPUT);
}

void loop() {
  // Read the state of the capacitive touch board
  touchState = digitalRead(TOUCH_PIN);
  unsigned long currentMillis = millis();

  // If a touch is detected count licks
  if ((touchState == HIGH) & (lick_flag == 0) &
      (currentMillis - previousMillis >= interval)) {
    previousMillis = currentMillis;
    lick_flag = 1;
    n_licks ++;
  }
  else if ((touchState == LOW) & (lick_flag == 1) &
      (currentMillis - previousMillis >= interval)) {
    lick_flag = 0;
  }

    // Check serial monitor for Unity input
  //if (Serial.available() > 0) {
  frame_state = char(Serial.read());
  if (frame_state == ',') {
    Serial.print(n_licks);
    Serial.println("");
    n_licks = 0; // reset lick count to zero each frame
    // }
  }
}
