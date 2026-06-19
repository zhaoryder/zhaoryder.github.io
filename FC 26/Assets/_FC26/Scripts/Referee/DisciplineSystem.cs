//=============================================================================
// 文件名：DisciplineSystem.cs
// 所属模块：Referee
// 命名空间：FC26.Referee
// 作用：纪律处罚系统单例。继承 MonoSingleton<DisciplineSystem>，全局唯一。
//       管理黄牌/红牌累计与罚下逻辑。
//       规则：累计两张黄牌自动升级为红牌罚下；直接红牌立即罚下。
//       记录每球员每队的牌数，罚下后该球员不可再上场。
// 备注：本脚本需挂载在场景中的 GameObject 上。
//       通过 EventBus 发布 CardShownEvent 通知 UI 与其他模块。
//=============================================================================
using System.Collections.Generic;
using UnityEngine;
using FC26.Core;

namespace FC26.Referee
{
    /// <summary>
    /// 纪律处罚系统单例：管理黄红牌累计与罚下。
    /// </summary>
    public class DisciplineSystem : MonoSingleton<DisciplineSystem>
    {
        // ===== 球员牌数记录 =====
        // 按球员 ID 存储黄牌数
        private readonly Dictionary<int, int> _yellowCards = new Dictionary<int, int>();

        // 按球员 ID 存储是否已被红牌罚下
        private readonly Dictionary<int, bool> _sentOff = new Dictionary<int, bool>();

        // 按球员 ID 存储所属队伍 ID
        private readonly Dictionary<int, int> _playerTeamMap = new Dictionary<int, int>();

        // ===== 统计 =====
        // 按队伍 ID 存储黄牌总数
        private readonly Dictionary<int, int> _teamYellowCards = new Dictionary<int, int>();

        // 按队伍 ID 存储红牌总数
        private readonly Dictionary<int, int> _teamRedCards = new Dictionary<int, int>();

        /// <summary>
        /// 注册球员到队伍的映射关系。
        /// 在比赛开始、球员创建后由 MatchManager 或 PlayerFactory 调用。
        /// </summary>
        /// <param name="playerId">球员 ID</param>
        /// <param name="teamId">队伍 ID（0=主队，1=客队）</param>
        public void RegisterPlayer(int playerId, int teamId)
        {
            _playerTeamMap[playerId] = teamId;
        }

        /// <summary>
        /// 向指定球员出示黄牌。
        /// 累计两张黄牌自动升级为红牌罚下。
        /// </summary>
        /// <param name="playerId">球员 ID</param>
        public void ShowYellowCard(int playerId)
        {
            // 已被罚下的球员不再出牌
            if (IsSentOff(playerId))
            {
                Debug.LogWarning($"[DisciplineSystem] 球员 {playerId} 已被罚下，无法再次出示黄牌。");
                return;
            }

            // 累计黄牌数
            if (!_yellowCards.ContainsKey(playerId))
            {
                _yellowCards[playerId] = 0;
            }
            _yellowCards[playerId]++;

            // 获取队伍 ID
            int teamId = GetPlayerTeamId(playerId);

            // 累计队伍黄牌总数
            if (!_teamYellowCards.ContainsKey(teamId))
            {
                _teamYellowCards[teamId] = 0;
            }
            _teamYellowCards[teamId]++;

            // 发布黄牌事件
            EventBus.Publish(new CardShownEvent
            {
                PlayerId = playerId,
                IsRed = false,
                TeamId = teamId
            });

            Debug.Log($"[DisciplineSystem] 球员 {playerId}（队伍 {teamId}）获得黄牌（累计 {_yellowCards[playerId]} 张）。");

            // 检查是否累计两张黄牌 → 升级为红牌
            if (_yellowCards[playerId] >= 2)
            {
                Debug.Log($"[DisciplineSystem] 球员 {playerId} 累计两张黄牌，升级为红牌罚下。");
                SendOff(playerId, teamId, false); // false=两黄变红，不是直接红牌
            }
        }

        /// <summary>
        /// 向指定球员出示红牌，直接罚下。
        /// </summary>
        /// <param name="playerId">球员 ID</param>
        public void ShowRedCard(int playerId)
        {
            // 已被罚下的球员不再出牌
            if (IsSentOff(playerId))
            {
                Debug.LogWarning($"[DisciplineSystem] 球员 {playerId} 已被罚下，无法再次出示红牌。");
                return;
            }

            int teamId = GetPlayerTeamId(playerId);

            // 标记罚下
            SendOff(playerId, teamId, true); // true=直接红牌

            Debug.Log($"[DisciplineSystem] 球员 {playerId}（队伍 {teamId}）被直接红牌罚下。");
        }

        /// <summary>
        /// 获取球员当前黄牌数。
        /// </summary>
        /// <param name="playerId">球员 ID</param>
        /// <returns>黄牌数（0~2）</returns>
        public int GetYellowCardCount(int playerId)
        {
            if (_yellowCards.TryGetValue(playerId, out var count))
            {
                return count;
            }
            return 0;
        }

        /// <summary>
        /// 球员是否已被红牌罚下。
        /// </summary>
        /// <param name="playerId">球员 ID</param>
        /// <returns>true=已罚下，false=未罚下</returns>
        public bool IsSentOff(int playerId)
        {
            return _sentOff.TryGetValue(playerId, out var sentOff) && sentOff;
        }

        /// <summary>
        /// 获取指定队伍的黄牌总数。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        /// <returns>黄牌总数</returns>
        public int GetTeamYellowCards(int teamId)
        {
            if (_teamYellowCards.TryGetValue(teamId, out var count))
            {
                return count;
            }
            return 0;
        }

        /// <summary>
        /// 获取指定队伍的红牌总数。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        /// <returns>红牌总数</returns>
        public int GetTeamRedCards(int teamId)
        {
            if (_teamRedCards.TryGetValue(teamId, out var count))
            {
                return count;
            }
            return 0;
        }

        /// <summary>
        /// 获取球员所属队伍 ID。
        /// </summary>
        /// <param name="playerId">球员 ID</param>
        /// <returns>队伍 ID（未注册返回 -1）</returns>
        public int GetPlayerTeamId(int playerId)
        {
            if (_playerTeamMap.TryGetValue(playerId, out var teamId))
            {
                return teamId;
            }
            return -1;
        }

        /// <summary>
        /// 重置所有纪律记录。在比赛开始前调用。
        /// </summary>
        public void Reset()
        {
            _yellowCards.Clear();
            _sentOff.Clear();
            _playerTeamMap.Clear();
            _teamYellowCards.Clear();
            _teamRedCards.Clear();
        }

        // ====================================================================
        #region 内部方法

        /// <summary>
        /// 执行罚下操作。
        /// </summary>
        /// <param name="playerId">球员 ID</param>
        /// <param name="teamId">队伍 ID</param>
        /// <param name="isDirectRed">是否直接红牌（false=两黄变红）</param>
        private void SendOff(int playerId, int teamId, bool isDirectRed)
        {
            _sentOff[playerId] = true;

            // 累计队伍红牌总数
            if (!_teamRedCards.ContainsKey(teamId))
            {
                _teamRedCards[teamId] = 0;
            }
            _teamRedCards[teamId]++;

            // 发布红牌事件
            EventBus.Publish(new CardShownEvent
            {
                PlayerId = playerId,
                IsRed = true,
                TeamId = teamId
            });
        }

        #endregion
    }
}

//=============================================================================
// 以下为出牌事件结构体定义，置于 FC26.Core 命名空间以保持事件一致性。
//=============================================================================
namespace FC26.Core
{
    /// <summary>
    /// 出牌事件。当裁判出示黄/红牌时由 DisciplineSystem 发布。
    /// </summary>
    public struct CardShownEvent
    {
        /// <summary>被出示牌的球员 ID</summary>
        public int PlayerId;

        /// <summary>是否红牌（false=黄牌）</summary>
        public bool IsRed;

        /// <summary>球员所属队伍 ID</summary>
        public int TeamId;
    }
}
