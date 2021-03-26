using UnityEngine;
using UnityEngine.Assertions;

public static class RestPosition {
    #region Private Const Fields
    private const float offset = 0.15f;
    private const float angle = 10;
    private const float tolerance = 0.1f;
    private const float numOfRaysForDir = 15;
    #endregion

    #region Core Logic
    public static void FindDistanceAndRotation(Vector3 iconPos, ref float distance, ref float restRotationY) {
        Assert.IsTrue(360 % angle == 0);
        int dirs = (int)(360 / angle);
        float referenceDistance = 0;
        Vector3[] edges = FindEdges(dirs, iconPos, ref referenceDistance);

        if (edges != null) {
            int resultIdx = FindRotation(dirs, iconPos, edges);
            Vector3 rawDirection = iconPos - edges[resultIdx];
            Vector3 rotation = Quaternion.LookRotation(rawDirection, Vector3.up).eulerAngles;

            if (NeedToReverseDirection(iconPos, rawDirection))
                restRotationY = rotation.y + 180;
            else
                restRotationY = rotation.y;

            distance = referenceDistance;
        }
    }

    private static Vector3[] FindEdges(int dirs, Vector3 iconPos, ref float referenceDistance) {
        Vector3[] edges = new Vector3[dirs];
        GameObject rayCaster = new GameObject("RayCaster");
        rayCaster.hideFlags = HideFlags.DontSaveInEditor;
        Transform rayCasterTransform = rayCaster.transform;

        Physics.Raycast(iconPos, Vector3.down, out RaycastHit rHit, 2f);
        referenceDistance = rHit.distance;

        if (referenceDistance != 0) {
            for (int i = 0; i < dirs; i++) {
                rayCasterTransform.position = iconPos;
                rayCasterTransform.eulerAngles = new Vector3(0, i * angle, 0);

                for (int j = 0; j < numOfRaysForDir; j++) {
                    Vector3 currentPos = rayCasterTransform.position;

                    Physics.Raycast(currentPos, Vector3.down, out RaycastHit hit, 2f);

                    if (hit.transform != null) {
                        if (IsNotSimilarToRef(hit.distance, referenceDistance)) {
                            edges[i] = currentPos;
                            break;
                        }
                    }
                    else {
                        edges[i] = currentPos;
                        break;
                    }

                    rayCasterTransform.Translate(new Vector3(0, 0, offset), Space.Self);
                }
            }

            Object.DestroyImmediate(rayCaster);
            return edges;
        }

        Object.DestroyImmediate(rayCaster);
        return null;
    }
    private static int FindRotation(int dirs, Vector3 iconPos, Vector3[] edges) {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "RotationFinder";
        cube.hideFlags = HideFlags.DontSaveInEditor;
        Transform cubeTransform = cube.transform;

        cubeTransform.localScale = new Vector3(0.2f, 0.2f, 2.5f);

        Vector3 direction;
        float distance;
        int resultIdx = -1;
        int max = 0;
        for (int i = 0; i < dirs; i++) {
            direction = (iconPos - edges[i]).normalized;
            distance = Vector3.Distance(edges[i], iconPos);

            cube.transform.position = iconPos - direction * distance;
            cube.transform.rotation = Quaternion.LookRotation(Vector3.Cross(iconPos - edges[i], Vector3.up));

            int temp = 0;
            BoxCollider col = cube.GetComponent<BoxCollider>();
            for (int j = 0; j < dirs; j++) {
                if (PointInOABB(edges[j], col))
                    temp++;
            }

            if (temp > max) {
                max = temp;
                resultIdx = i;
            }
        }

        Object.DestroyImmediate(cube);

        return resultIdx;
    }
    #endregion

    #region Helper Methods
    private static bool NeedToReverseDirection(Vector3 iconPos, Vector3 rayCastDir, uint id = 0) {
        Physics.Raycast(iconPos, rayCastDir, out RaycastHit first, 2f);
        Physics.Raycast(iconPos, -rayCastDir, out RaycastHit second, 2f);

        if (first.distance != 0 && second.distance != 0)
            return second.distance > first.distance;
        else
            return second.distance == 0;
    }
    private static bool IsNotSimilarToRef(float distance, float referenceDistance) {
        return (distance > referenceDistance + tolerance) || (distance < referenceDistance - tolerance);
    }
    private static bool PointInOABB(Vector3 point, BoxCollider box) {
        point = box.transform.InverseTransformPoint(point) - box.center;

        float halfX = (box.size.x * 0.5f);
        float halfY = (box.size.y * 0.5f);
        float halfZ = (box.size.z * 0.5f);

        return point.x < halfX && point.x > -halfX && point.y < halfY && point.y > -halfY && point.z < halfZ && point.z > -halfZ;
    }
    #endregion
}