using System.Collections.Generic;
using PenguinWars.Battle;

namespace PenguinWars
{
    /// <summary>
    /// タイトルの後ろで流す小競り合いの台本（仕様書 §2.0）。味方はパレードの5体、敵は全キャラからランダム。
    /// あそびかたと同じデモの仕組み（GuideDemoDirector）で流すので、ここは「誰をいつどこに出すか」だけを作る
    /// </summary>
    public static class TitleBattleScript
    {
        // 城が落ちて止まる前・味方が画面の外へ抜ける前にやり直す長さ
        private const float LoopSeconds = 12f;
        private const int EnemyCount = 4;
        // 1体ずつ間をあけて出し、ぶつかり合いが途切れないようにする
        private const float AllyInterval = 1.2f;
        private const float EnemyInterval = 1.5f;
        // タイトル中のカメラ（だいたい X=-2〜14）の両端から歩いてきて、真ん中でぶつかる位置
        private const float AllyX = 1.5f;
        private const float EnemyX = 13f;

        public static GuideTopic Create(IReadOnlyList<int> allyNos, IReadOnlyList<int> enemyPool)
        {
            var actions = new List<GuideAction>();
            for (int i = 0; i < allyNos.Count; i++) actions.Add(GuideAction.Spawn(i * AllyInterval, Side.Left, allyNos[i], AllyX));
            for (int i = 0; i < EnemyCount && enemyPool.Count > 0; i++)
            {
                int no = enemyPool[UnityEngine.Random.Range(0, enemyPool.Count)];
                actions.Add(GuideAction.Spawn(i * EnemyInterval, Side.Right, no, EnemyX));
            }
            // デモ係は Time の早い順に読む
            actions.Sort((a, b) => a.Time.CompareTo(b.Time));
            return new GuideTopic(string.Empty, string.Empty, LoopSeconds, actions.ToArray());
        }
    }
}
