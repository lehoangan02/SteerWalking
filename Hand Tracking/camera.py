import cv2

def scan_cameras(max_index=15):
    for i in range(max_index):
        cap = cv2.VideoCapture(i, cv2.CAP_DSHOW)
        if cap.isOpened():
            ret, frame = cap.read()
            if ret:
                print(f"[OK] Camera index {i}")
                cv2.imshow(f"Camera {i}", frame)
                cv2.waitKey(1000)
                cv2.destroyAllWindows()
        cap.release()

scan_cameras(20)
