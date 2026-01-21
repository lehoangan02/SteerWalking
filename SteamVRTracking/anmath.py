from sympy import Point3D, Matrix, cos, sin, rad
from sympy.geometry import Plane, Circle

def make_point(x, y, z):
    return Point3D(x, y, z)

def is_three_point_line(p1, p2, p3):
    return Point3D.are_collinear(p1, p2, p3)

def three_point_surface(p1, p2, p3):
    if Point3D.are_collinear(p1, p2, p3):
        raise ValueError("Points are collinear; no unique plane.")

    plane = Plane(p1, p2, p3)

    A, B, C = plane.normal_vector
    D = -(A * p1.x + B * p1.y + C * p1.z)

    return A, B, C, D

def three_point_circle(p1, p2, p3):
    if Point3D.are_collinear(p1, p2, p3):
        raise ValueError("Points are collinear; no unique circle.")

    plane = Plane(p1, p2, p3)

    a = Matrix([p1.x, p1.y, p1.z])
    b = Matrix([p2.x, p2.y, p2.z])
    c = Matrix([p3.x, p3.y, p3.z])

    ab = b - a
    ac = c - a

    n = ab.cross(ac)

    A = Matrix.vstack(
        ab.T,
        ac.T,
        n.T
    )

    B = Matrix([
        ab.dot((a + b) / 2),
        ac.dot((a + c) / 2),
        n.dot(a)
    ])

    center_vec = A.LUsolve(B)

    center = Point3D(center_vec[0], center_vec[1], center_vec[2])
    radius = center.distance(p1)

    return center, radius, plane

def rotate_around_ground_normal(point, origin, degree):
    theta = rad(degree)

    R = Matrix([
        [cos(theta), 0, sin(theta)],
        [0,          1, 0],
        [-sin(theta),0, cos(theta)]
    ])

    v = Matrix([
        point.x - origin.x,
        point.y - origin.y,
        point.z - origin.z
    ])

    vr = R * v

    return Point3D(
        vr[0] + origin.x,
        vr[1] + origin.y,
        vr[2] + origin.z
    )

if __name__ == "__main__":
    p1 = make_point(1, 0, 0)
    p2 = make_point(0, 1, 0)
    p3 = make_point(0, 0, 1)

    print("Collinear:", is_three_point_line(p1, p2, p3))

    A, B, C, D = three_point_surface(p1, p2, p3)
    print("Plane: Ax+By+Cz+D=0 ->", A, B, C, D)

    center, radius, plane = three_point_circle(p1, p2, p3)
    print("Circle center:", center)
    print("Circle radius:", radius)
    print("Circle plane:", plane)

    p4 = make_point(1, 0, 0)
    origin = make_point(0, 1, 0)
    degree = 90
    rotated_point = rotate_around_ground_normal(p4, origin, degree)
    print("Rotated point:", rotated_point)
