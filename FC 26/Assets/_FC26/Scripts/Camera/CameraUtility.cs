//=============================================================================
// 文件名：CameraUtility.cs
// 所属模块：Camera
// 命名空间：FC26.Camera
// 作用：摄像机相关静态工具方法集合。提供屏幕坐标与球场平面（y=0）的交点换算、
//       输入方向到摄像机相对世界方向的转换、以及球员到鼠标位置的方向计算。
//       全部为纯计算方法，不依赖 MonoBehaviour 生命周期，便于复用与单元测试。
// 备注：球场平面约定为 y=0（地面），所有交点计算均基于该平面。
//       依赖主摄像机 Camera.main，调用前需确保场景中存在 MainCamera。
//=============================================================================
using UnityEngine;

namespace FC26.Camera
{
    /// <summary>
    /// 摄像机静态工具类：屏幕坐标与球场平面交点、输入方向相对摄像机变换等。
    /// </summary>
    public static class CameraUtility
    {
        // 球场地面平面高度（y=0），所有交点计算基于此平面。
        private const float GroundY = 0f;

        /// <summary>
        /// 屏幕坐标转球场平面（y=0）交点。
        /// 从主摄像机发射一条穿过屏幕坐标的射线，与 y=0 平面求交。
        /// 典型用途：鼠标点击地面位置、球员到鼠标方向计算。
        /// </summary>
        /// <param name="screenPos">屏幕坐标（z 分量忽略，使用 Camera.main.ScreenPointToRay）</param>
        /// <returns>射线与地面平面的交点世界坐标；若射线与平面平行或主摄像机不存在返回 Vector3.zero</returns>
        public static Vector3 ScreenToGroundPoint(Vector3 screenPos)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                return Vector3.zero;
            }

            Ray ray = cam.ScreenPointToRay(screenPos);

            // 射线方程：P = origin + t * dir
            // 平面方程：P.y = GroundY
            // 求解：origin.y + t * dir.y = GroundY → t = (GroundY - origin.y) / dir.y
            // 当 dir.y 接近 0（射线与平面平行）时无交点。
            if (Mathf.Abs(ray.direction.y) < 1e-6f)
            {
                return Vector3.zero;
            }

            float t = (GroundY - ray.origin.y) / ray.direction.y;

            // t < 0 表示交点在射线反方向（摄像机背后），视为无效
            if (t < 0f)
            {
                return Vector3.zero;
            }

            return ray.origin + ray.direction * t;
        }

        /// <summary>
        /// 将二维输入方向（WASD）转为相对摄像机的世界方向。
        /// 约定输入空间：x=-1 左, x=+1 右, y=-1 后, y=+1 前。
        /// "前"指摄像机正前方在地面上的投影方向，"右"指摄像机右侧在地面上的投影方向。
        /// 这样玩家按 W 时角色会朝摄像机看向的方向移动，符合第三人称操作直觉。
        /// </summary>
        /// <param name="input">二维输入方向（已归一化或未归一化均可）</param>
        /// <returns>世界空间水平方向（y=0，已归一化）；若输入为零或主摄像机不存在返回 Vector3.zero</returns>
        public static Vector3 GetMoveDirectionRelativeToCamera(Vector2 input)
        {
            // 输入为零直接返回零向量，避免无意义计算
            if (input.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                return Vector3.zero;
            }

            // 取摄像机正前方与正右方，投影到地面（y=0）并归一化。
            Vector3 camForward = cam.transform.forward;
            Vector3 camRight = cam.transform.right;

            // 投影到水平面：清除 y 分量
            camForward.y = 0f;
            camRight.y = 0f;

            // 摄像机几乎垂直俯视时 forward 投影可能为零，回退到 +z
            if (camForward.sqrMagnitude < 1e-6f)
            {
                camForward = Vector3.forward;
            }
            if (camRight.sqrMagnitude < 1e-6f)
            {
                camRight = Vector3.right;
            }

            camForward.Normalize();
            camRight.Normalize();

            // input.y 对应前/后（W/S），input.x 对应右/左（D/A）
            Vector3 worldDir = camForward * input.y + camRight * input.x;

            // 归一化保证对角线移动速度与直线一致
            if (worldDir.sqrMagnitude > 1e-6f)
            {
                worldDir.Normalize();
            }

            return worldDir;
        }

        /// <summary>
        /// 计算球员到当前鼠标位置的球场方向（水平方向，已归一化）。
        /// 典型用途：传球/射门方向由鼠标位置决定（鼠标指向哪里就踢向哪里）。
        /// </summary>
        /// <param name="playerPos">球员世界坐标</param>
        /// <returns>从球员指向鼠标地面交点的水平方向（已归一化）；若无法计算返回 Vector3.zero</returns>
        public static Vector3 GetDirectionFromPlayerToMouse(Vector3 playerPos)
        {
            // 取当前鼠标屏幕坐标（Input.mousePosition 的 z 分量无意义，置 0）
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 groundPoint = ScreenToGroundPoint(mouseScreen);

            // 计算水平方向
            Vector3 dir = groundPoint - playerPos;
            dir.y = 0f;

            if (dir.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            return dir.normalized;
        }
    }
}
