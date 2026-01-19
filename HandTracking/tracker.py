import os
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"
import cv2
import mediapipe as mp
import socket

# =============================
# NETWORK SETUP
# =============================
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1", 5052)

# =============================
# MEDIAPIPE SETUP
# =============================
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    model_complexity=0, 
    min_detection_confidence=0.6,
    min_tracking_confidence=0.6
)

cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)

while True:
    ret, frame = cap.read()
    if not ret: break

    frame = cv2.flip(frame, 1)
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    result = hands.process(rgb)

    # Initialize list with 6 zeros [Lx, Ly, Lz, Rx, Ry, Rz]
    # 0.0 serves as a "Hand Not Found" flag
    data = [0.0] * 6 

    if result.multi_hand_landmarks and result.multi_handedness:
        # Zip allows us to loop through landmarks and labels together
        for idx, hand_handedness in enumerate(result.multi_handedness):
            # Get the label: "Left" or "Right"
            label = hand_handedness.classification[0].label 
            
            # Get the specific landmark (Index 9 = Middle Finger MCP)
            lm = result.multi_hand_landmarks[idx].landmark[9]
            
            # SORTING LOGIC
            if label == "Left":
                # Fill first 3 slots
                data[0] = lm.x
                data[1] = lm.y
                data[2] = lm.z
            if label == "Right":
                # Fill last 3 slots
                data[3] = lm.x
                data[4] = lm.y
                data[5] = lm.z

            # Visual Feedback
            h, w, _ = frame.shape
            cx, cy = int(lm.x * w), int(lm.y * h)
            # Blue for Left, Red for Right
            color = (255, 0, 0) if label == "Left" else (0, 0, 255)
            cv2.circle(frame, (cx, cy), 10, color, cv2.FILLED)

    # Always send exactly 6 values
    message = ",".join(map(str, data))
    sock.sendto(message.encode(), serverAddressPort)

    cv2.imshow("Locked Hand Tracker", frame)
    if cv2.waitKey(1) & 0xFF == 27: break

cap.release()
cv2.destroyAllWindows()