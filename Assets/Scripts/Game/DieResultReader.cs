using UnityEngine;

namespace Game
{
    /// <summary>
    /// Reads which face of a die is pointing up after it settles.
    /// Attach this to each die prefab.
    ///
    /// HOW TO CONFIGURE:
    ///   Each _faceNormal field is a local-space direction pointing OUTWARD from that numbered face.
    ///   After the die settles, the face whose world-space normal has the highest dot product with
    ///   Vector3.up is the top face.
    ///
    ///   Default values assume the die mesh's local axes are:
    ///     Face 1 = +Y,  Face 2 = +Z,  Face 3 = +X
    ///     Face 4 = -X,  Face 5 = -Z,  Face 6 = -Y
    ///
    ///   If you get wrong values in play, select a settled die in the Scene view –
    ///   the Gizmo arrows show where each face is pointing. Rotate each vector in the
    ///   Inspector until the arrows match your mesh.
    ///
    ///   Standard Western die: opposite faces sum to 7 (1↔6, 2↔5, 3↔4).
    /// </summary>
    public class DieResultReader : MonoBehaviour
    {
        [Header("Face Normals (local space, outward from each numbered face)")]
        [Tooltip("Local direction pointing out of the face labelled '1'.")]
        [SerializeField] private Vector3 _face1Normal = Vector3.up;
        [Tooltip("Local direction pointing out of the face labelled '2'.")]
        [SerializeField] private Vector3 _face2Normal = Vector3.forward;
        [Tooltip("Local direction pointing out of the face labelled '3'.")]
        [SerializeField] private Vector3 _face3Normal = Vector3.right;
        [Tooltip("Local direction pointing out of the face labelled '4'.")]
        [SerializeField] private Vector3 _face4Normal = Vector3.left;
        [Tooltip("Local direction pointing out of the face labelled '5'.")]
        [SerializeField] private Vector3 _face5Normal = Vector3.back;
        [Tooltip("Local direction pointing out of the face labelled '6'.")]
        [SerializeField] private Vector3 _face6Normal = Vector3.down;

        /// <summary>
        /// Returns 1–6 for the face pointing most directly upward.
        /// Call only after the die has settled.
        /// </summary>
        public int GetTopFaceValue()
        {
            Vector3[] normals =
            {
                transform.TransformDirection(_face1Normal.normalized),
                transform.TransformDirection(_face2Normal.normalized),
                transform.TransformDirection(_face3Normal.normalized),
                transform.TransformDirection(_face4Normal.normalized),
                transform.TransformDirection(_face5Normal.normalized),
                transform.TransformDirection(_face6Normal.normalized),
            };

            int   bestValue = 1;
            float bestDot   = float.MinValue;

            for (int i = 0; i < normals.Length; i++)
            {
                float dot = Vector3.Dot(normals[i], Vector3.up);
                if (dot > bestDot)
                {
                    bestDot   = dot;
                    bestValue = i + 1;
                }
            }

            return bestValue;
        }

#if UNITY_EDITOR
        // Draws arrows in the Scene view so face mapping is easy to verify and fix.
        private void OnDrawGizmosSelected()
        {
            Vector3[] locals = { _face1Normal, _face2Normal, _face3Normal,
                                 _face4Normal, _face5Normal, _face6Normal };

            for (int i = 0; i < locals.Length; i++)
            {
                Gizmos.color = Color.Lerp(Color.cyan, Color.red, i / 5f);
                Vector3 world = transform.TransformDirection(locals[i].normalized);
                Gizmos.DrawRay(transform.position, world * 0.4f);
            }
        }
#endif
    }
}
