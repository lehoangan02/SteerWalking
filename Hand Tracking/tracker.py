import os
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"
import cv2
import mediapipe as mp
import socket

# =============================
# OPTIMIZED NETWORK SETUP
# =============================
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1", 5052)

# =============================
# FAST MEDIAPIPE SETUP (Complexity 0)
# =============================
mp_hands = mp.solutions.hands
# model_complexity=0 is the "Lite" version (Fastest)
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    model_complexity=0, 
    min_detection_confidence=0.6,
    min_tracking_confidence=0.6
)

cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

while True:
    ret, frame = cap.read()
    if not ret: break

    frame = cv2.flip(frame, 1)
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    result = hands.process(rgb)

    data = []

    if result.multi_hand_landmarks:
        for hand_landmarks in result.multi_hand_landmarks:
            # Landmark 9 is the Middle Finger MCP (center of hand)
            lm = hand_landmarks.landmark[9]
            h, w, _ = frame.shape
            
            # Send raw normalized coordinates (0.0 to 1.0)
            data.extend([lm.x, lm.y, lm.z]) 

            # DRAW ONLY ONE CIRCLE (Fastest)
            cx, cy = int(lm.x * w), int(lm.y * h)
            cv2.circle(frame, (cx, cy), 10, (0, 255, 0), cv2.FILLED)

    # Send data to Unity
    if data:
        message = ",".join(map(str, data))
        sock.sendto(message.encode(), serverAddressPort)

    cv2.imshow("Fast Single Point Tracker", frame)
    if cv2.waitKey(1) & 0xFF == 27: break

cap.release()
cv2.destroyAllWindows()