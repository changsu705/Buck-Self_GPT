using System;
using System.Linq;
using System.Collections.Generic;
using Buckshot.Contracts;

namespace Buckshot.Core
{
    /// <summary>읽기 전용 어댑터</summary>
    public class ReadonlyGameStateAdapter : IReadonlyGameState
    {
        private readonly IGameStateStore _store;
        private readonly Func<int, int> _opponentFn;

        public ReadonlyGameStateAdapter(IGameStateStore store, Func<int, int> opponentFn)
        {
            _store = store;
            _opponentFn = opponentFn;
        }

        public int CurrentTurnActor => _store.CurrentTurnActor;
        public int ShellIndex => _store.ShellIndex;
        public ShellType[] Shells => _store.Shells;
        public bool HasShellLeft => ShellIndex < Shells.Length;

        public int GetHp(int actor) => _store.GetHp(actor);
        public int GetOpponent(int me) => _opponentFn(me);
    }

    /// <summary>기본 덱 빌더: 시드 기반 셔플</summary>
    public class DefaultShellDeckBuilder : IShellDeckBuilder
    {
        public ShellType[] Build(int seed, int liveCount, int blankCount)
        {
            var list = new List<ShellType>(liveCount + blankCount);
            for (int i = 0; i < liveCount; i++) list.Add(ShellType.Live);
            for (int i = 0; i < blankCount; i++) list.Add(ShellType.Blank);

            var rng = new System.Random(seed);
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list.ToArray();
        }
    }

    /// <summary>기본 첫 턴 정책: 시드 기반 랜덤</summary>
    public class RandomFirstTurnPolicy : IFirstTurnPolicy
    {
        public int PickFirstActor(IReadOnlyList<int> actorNumbers, int seed)
        {
            if (actorNumbers == null || actorNumbers.Count == 0)
                throw new ArgumentException("actor list empty");

            // uint 상수와 시드를 XOR → int로 변환
            uint mixed = ((uint)seed) ^ 0x9e3779b9u;
            int mixedInt = unchecked((int)mixed);

            var rng = new System.Random(mixedInt);
            int idx = rng.Next(actorNumbers.Count);
            return actorNumbers[idx];
        }
    }

    /// <summary>룰 엔진: 샷 판정만 담당</summary>
    public class BasicRuleEngine : IRuleEngine
    {
        public ShotResult ResolveShot(IReadonlyGameState state, ShootRequest request)
        {
            if (request.ShooterActor != state.CurrentTurnActor)
                throw new InvalidOperationException("Not your turn");

            if (!state.HasShellLeft)
            {
                return new ShotResult
                {
                    ShooterActor = request.ShooterActor,
                    TargetActor = request.TargetActor,
                    Shell = ShellType.Blank,
                    NewTargetHp = state.GetHp(request.TargetActor),
                    IsRoundOver = true,
                    NextTurnActor = state.CurrentTurnActor
                };
            }

            var shell = state.Shells[state.ShellIndex];
            int targetHp = state.GetHp(request.TargetActor);

            if (shell == ShellType.Live)
            {
                targetHp = Math.Max(0, targetHp - 1);
                int next = state.GetOpponent(request.ShooterActor);

                return new ShotResult
                {
                    ShooterActor = request.ShooterActor,
                    TargetActor = request.TargetActor,
                    Shell = ShellType.Live,
                    NewTargetHp = targetHp,
                    IsRoundOver = false,
                    NextTurnActor = next
                };
            }
            else
            {
                return new ShotResult
                {
                    ShooterActor = request.ShooterActor,
                    TargetActor = request.TargetActor,
                    Shell = ShellType.Blank,
                    NewTargetHp = targetHp,
                    IsRoundOver = false,
                    NextTurnActor = request.ShooterActor
                };
            }
        }
    }

    public static class RoundSetup
    {
        public static void InitializeRound(
            IGameStateStore store,
            IShellDeckBuilder deckBuilder,
            IFirstTurnPolicy turnPolicy,
            int seed, int live, int blank)
        {
            var shells = deckBuilder.Build(seed, live, blank);
            store.SetShells(shells);
            store.SetShellIndex(0);

            var actors = store.AllActors.OrderBy(x => x).ToList();
            int first = turnPolicy.PickFirstActor(actors, seed);
            store.SetCurrentTurn(first);
        }

        public static void ApplyShotResult(IGameStateStore store, ShotResult result)
        {
            store.SetHp(result.TargetActor, result.NewTargetHp);

            // 인덱스 오버런 방지 (클램프)
            int next = store.ShellIndex + 1;
            int max = store.Shells?.Length ?? 0;
            if (next > max) next = max;
            store.SetShellIndex(next);

            store.SetCurrentTurn(result.NextTurnActor);
        }

    }
}
