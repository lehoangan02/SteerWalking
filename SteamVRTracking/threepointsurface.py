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