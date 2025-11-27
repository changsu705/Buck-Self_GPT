using System;
using System.Collections.Generic;
using System.Linq;
using Buckshot.Contracts;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Buckshot.PhotonInfra
{
    /// <summary>
    /// Photon 룸 커스텀 프로퍼티를 IGameStateStore로 어댑트한다.
    /// 상태의 '사실상의 원천'은 룸 프로퍼티이며, 모든 클라이언트가 동일한 값을 공유한다.
    /// 단일 책임: 저장/조회만 담당한다.
    /// </summary>
    public class PhotonRoomStore : IGameStateStore
    {
        // 커스텀 프로퍼티 키 관리
        public const string KEY_TURN = "turnActor";
        public const string KEY_SHELLS = "shells";
        public const string KEY_SHELL_IDX = "shellIdx";
        public const string KEY_HP_PREFIX = "hp_";

        private readonly Room _room;
        private ShellType[] _cachedShells; // 문자열 직렬화 해제 캐시

        public PhotonRoomStore(Room room)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
        }

        public int CurrentTurnActor => TryGet<int>(KEY_TURN, -1);
        public int ShellIndex => TryGet<int>(KEY_SHELL_IDX, 0);

        public ShellType[] Shells
        {
            get
            {
                if (_cachedShells != null) return _cachedShells;

                string s = TryGet<string>(KEY_SHELLS, string.Empty);
                if (string.IsNullOrEmpty(s)) return Array.Empty<ShellType>();
                _cachedShells = s.Split(',').Select(x => (ShellType)int.Parse(x)).ToArray();
                return _cachedShells;
            }
        }

        public IEnumerable<int> AllActors
            => _room?.Players?.Values?.Select(p => p.ActorNumber) ?? Array.Empty<int>();

        public int GetHp(int actor) => TryGet<int>(KEY_HP_PREFIX + actor, 0);

        public void SetCurrentTurn(int actor) => Set(new Hashtable { [KEY_TURN] = actor });

        public void SetShellIndex(int index) => Set(new Hashtable { [KEY_SHELL_IDX] = index });

        public void SetShells(ShellType[] shells)
        {
            _cachedShells = shells ?? Array.Empty<ShellType>();
            string serialized = string.Join(",", _cachedShells.Select(x => ((int)x).ToString()));
            Set(new Hashtable { [KEY_SHELLS] = serialized });
        }

        public void SetHp(int actor, int hp) => Set(new Hashtable { [KEY_HP_PREFIX + actor] = hp });

        // -------- 내부 유틸리티 --------

        /// <summary>
        /// 룸 커스텀 프로퍼티 읽기 헬퍼.
        /// 형변환 실패 시 기본값을 반환한다.
        /// </summary>
        private T TryGet<T>(string key, T fallback)
        {
            if (_room == null || _room.CustomProperties == null) return fallback;
            if (!_room.CustomProperties.TryGetValue(key, out object v)) return fallback;
            try { return (T)v; } catch { return fallback; }
        }

        /// <summary>
        /// 룸 커스텀 프로퍼티 쓰기 헬퍼.
        /// </summary>
        private void Set(Hashtable table)
        {
            _room.SetCustomProperties(table);
        }
    }
}
