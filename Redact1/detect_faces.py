#!/usr/bin/env python3
"""Face detection script using OpenCV Haar cascade."""
import sys
import cv2
import json

def detect_faces(image_path, cascade_path):
    """Detect faces in an image and return bounding boxes as JSON."""
    # Load image
    img = cv2.imread(image_path)
    if img is None:
        return json.dumps({"error": "Failed to load image", "faces": []})

    height, width = img.shape[:2]

    # Load cascade
    face_cascade = cv2.CascadeClassifier(cascade_path)
    if face_cascade.empty():
        return json.dumps({"error": "Failed to load cascade", "faces": []})

    # Convert to grayscale
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    gray = cv2.equalizeHist(gray)

    # Detect faces - use original sensitivity but filter false positives
    faces = face_cascade.detectMultiScale(
        gray,
        scaleFactor=1.1,
        minNeighbors=5,
        minSize=(40, 40)
    )

    # Filter out false positives: remove detections that are directly below another
    # (e.g., chest/throat area detected below a face)
    filtered_faces = []
    for (x, y, w, h) in faces:
        is_below_another = False
        for (x2, y2, w2, h2) in faces:
            if (x, y, w, h) == (x2, y2, w2, h2):
                continue
            # Check if this detection is directly below another (overlapping x-range)
            x_overlap = (x < x2 + w2) and (x + w > x2)
            is_below = y > y2 + h2 * 0.5  # This face starts below the middle of the other
            close_vertically = y < y2 + h2 * 1.5  # And is close enough to be a false positive
            if x_overlap and is_below and close_vertically:
                is_below_another = True
                break
        if not is_below_another:
            filtered_faces.append((x, y, w, h))

    # Convert to normalized coordinates
    result = []
    for (x, y, w, h) in filtered_faces:
        result.append({
            "x": x / width,
            "y": y / height,
            "width": w / width,
            "height": h / height
        })

    return json.dumps({"faces": result, "count": len(result)})

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(json.dumps({"error": "Usage: detect_faces.py <image_path> <cascade_path>", "faces": []}))
        sys.exit(1)

    print(detect_faces(sys.argv[1], sys.argv[2]))
