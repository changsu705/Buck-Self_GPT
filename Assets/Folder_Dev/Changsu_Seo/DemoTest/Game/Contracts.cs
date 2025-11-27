using System;
using System.Collections.Generic;

namespace Buckshot.Contracts
{
    /// <summary>탄 종류</summary>
    public enum ShellType { Blank = 0, Live = 1 }

    /// <summary>샷 요청 DTO (클라이언트 → 호스트)</summary>
    public struct ShootRequest
    {
        public int ShooterActor;   // 쏜 플레이어
        public int TargetActor;    // 맞는 대상
    }

    /// <summary>샷 판정 결과 DTO (호스트 → 모든 클라이언트)</summary>
    public struct ShotResult
    {
        public int ShooterActor;
        public int TargetActor;
        public ShellType Shell;
        public int NewTargetHp;
        public bool IsRoundOver;
        public int NextTurnActor;
    }

    public interface IReadonlyGameState
    {
        int CurrentTurnActor { get; }
        int ShellIndex { get; }
        ShellType[] Shells { get; }
        int GetHp(int actor);
        int GetOpponent(int me);
        bool HasShellLeft { get; }
    }

    public interface IGameStateStore
    {
        int CurrentTurnActor { get; }
        int ShellIndex { get; }
        ShellType[] Shells { get; }
        int GetHp(int actor);

        void SetCurrentTurn(int actor);
        void SetShellIndex(int index);
        void SetShells(ShellType[] shells);
        void SetHp(int actor, int hp);

        IEnumerable<int> AllActors { get; }
    }

    public interface IShellDeckBuilder
    {
        ShellType[] Build(int seed, int liveCount, int blankCount);
    }

    public interface IFirstTurnPolicy
    {
        int PickFirstActor(IReadOnlyList<int> actorNumbers, int seed);
    }

    public interface IRuleEngine
    {
        ShotResult ResolveShot(IReadonlyGameState state, ShootRequest request);
    }

    public interface INetTransport
    {
        void SendShootRequestToHost(ShootRequest req);
        void BroadcastShotResult(ShotResult result);
        void BroadcastNewRound();
    }
}
