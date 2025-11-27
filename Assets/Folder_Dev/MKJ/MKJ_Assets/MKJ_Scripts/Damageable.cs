using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [Tooltip("캐릭터의 현재 체력입니다.")]
    public int currentHp = 1;

    /// <summary>
    /// 데미지를 받아 체력을 감소시킵니다.
    /// </summary>
    /// <param name="damage">입을 데미지 양</param>
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log(gameObject.name + "이(가) " + damage + "의 데미지를 입었습니다! 현재 체력: " + currentHp);

        // 체력이 0 이하가 되면 Die 함수 호출
        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 캐릭터가 쓰러졌을 때 처리할 내용을 담습니다.
    /// </summary>
    private void Die()
    {
        Debug.Log(gameObject.name + "이(가) 쓰러졌습니다.");
        // 예: 오브젝트를 비활성화하여 화면에서 사라지게 함
        gameObject.SetActive(false);
        // 또는 애니메이션 재생, 파티클 효과 등을 여기에 추가할 수 있습니다.
    }
}