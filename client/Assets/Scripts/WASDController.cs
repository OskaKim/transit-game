using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityChan
{
    /// <summary>
    /// WASDキーでユニティちゃんを移動させるコントローラー。
    /// New Input System の Keyboard.current を直接ポーリングする。
    /// </summary>
    public class WASDController : MonoBehaviour
    {
        [Header("移動設定")]
        [SerializeField] float m_moveSpeed = 2.0f;
        [SerializeField] float m_rotateSpeed = 720f; // deg/sec

        [Header("カメラ相対移動")]
        [Tooltip("Trueにするとカメラの向きを基準に移動方向を計算する")]
        [SerializeField] bool m_cameraRelative = true;

        Animator m_animator;

        void Awake()
        {
            m_animator = GetComponent<Animator>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // 入力取得
            float h = 0f, v = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;

            Vector3 inputDir = new Vector3(h, 0f, v).normalized;
            if (inputDir.sqrMagnitude < 0.01f) return;

            // カメラ相対に変換
            Vector3 moveDir = inputDir;
            if (m_cameraRelative && Camera.main != null)
            {
                var camFwd = Camera.main.transform.forward;
                var camRight = Camera.main.transform.right;
                camFwd.y = 0f; camFwd.Normalize();
                camRight.y = 0f; camRight.Normalize();
                moveDir = camFwd * inputDir.z + camRight * inputDir.x;
            }

            // 移動
            transform.position += moveDir * m_moveSpeed * Time.deltaTime;

            // キャラをなめらかに移動方向へ回転
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, m_rotateSpeed * Time.deltaTime);
            }
        }
    }
}
