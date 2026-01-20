import math


class surface:
    # surface format: Ax+By+Cz+D = 0 with A^2+B^2+C^2≠0
    def __init__(self, A, B, C, D):
        self.A = A
        self.B = B
        self.C = C
        self.D = D
        if A == 0 and B == 0 and C == 0:
            raise ValueError("Invalid surface: A, B, C cannot all be zero.")
class point3D:
    def __init__(self, x, y, z):
        self.x = x
        self.y = y
        self.z = z

def three_point_surface(p1, p2, p3):
    # p1, p2, p3 are point3D objects
    x1, y1, z1 = p1.x, p1.y, p1.z
    x2, y2, z2 = p2.x, p2.y, p2.z
    x3, y3, z3 = p3.x, p3.y, p3.z

    # Vectors from p1 to p2 and p1 to p3
    v1 = (x2 - x1, y2 - y1, z2 - z1)
    v2 = (x3 - x1, y3 - y1, z3 - z1)

    # Cross product to find normal vector (A, B, C)
    A = v1[1] * v2[2] - v1[2] * v2[1]
    B = v1[2] * v2[0] - v1[0] * v2[2]
    C = v1[0] * v2[1] - v1[1] * v2[0]

    # Calculate D using point p1
    D = -(A * x1 + B * y1 + C * z1)

    return surface(A, B, C, D)

class circle3D:
    # circle format: (x - A)^2 + (y - B)^2 + (z - C)^2 = R^2
    def __init__(self, A, B, C, R):
        self.A = A
        self.B = B
        self.C = C
        self.R = R
        if R < 0:
            raise ValueError("Invalid circle: Radius cannot be negative.")
    def getCenter(self):
        return point3D(self.A, self.B, self.C)

def three_point_circle(p1, p2, p3):
    # p1, p2, p3 are point3D objects
    import numpy as np
    
    P1 = np.array([p1.x, p1.y, p1.z])
    P2 = np.array([p2.x, p2.y, p2.z])
    P3 = np.array([p3.x, p3.y, p3.z])
    
    # Vectors
    v1 = P2 - P1
    v2 = P3 - P1
    
    # Normal to the plane
    normal = np.cross(v1, v2)
    if np.linalg.norm(normal) == 0:
        raise ValueError("The points are collinear; no unique circle exists.")
    
    # Perpendicular bisector directions (in the plane)
    n1 = np.cross(v1, normal)
    n2 = np.cross(v2, normal)
    
    # Midpoints
    mid1 = (P1 + P2) / 2
    mid2 = (P1 + P3) / 2
    
    # Solve for intersection: mid1 + t*n1 = mid2 + s*n2
    # Using least squares to find t
    w = mid2 - mid1
    
    # t = (w · n2 × normal) / (n1 · n2 × normal)
    n2_cross_normal = np.cross(n2, normal)
    denom = np.dot(n1, n2_cross_normal)
    
    if abs(denom) < 1e-10:
        raise ValueError("Cannot compute circle center.")
    
    t = np.dot(w, n2_cross_normal) / denom
    
    # Center of the circle
    center = mid1 + t * n1
    
    # Radius
    R = np.linalg.norm(center - P1)
    
    return circle3D(center[0], center[1], center[2], R)