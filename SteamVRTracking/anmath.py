from sympy import Point3D
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
    D = plane.equation().subs({plane.x: 0, plane.y: 0, plane.z: 0})

    return A, B, C, D

def three_point_circle(p1, p2, p3):
    if Point3D.are_collinear(p1, p2, p3):
        raise ValueError("Points are collinear; no unique circle.")

    c = Circle(p1, p2, p3)
    return c.center, c.radius, c.plane

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
