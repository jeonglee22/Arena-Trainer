using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class ArenaAgent : Agent
{
    [SerializeField] private Transform enemy;        // 적 오브젝트 연결
    [SerializeField] private float moveSpeed = 3f;   // 이동 속도
    private Rigidbody2D rb;

    private float attackRange = 1.5f; // 공격 범위
    private float attackCooldown = 0.5f; // 공격 쿨다운
    private float attackTimer = 0f;

    private float previousDistance = 0f; // 이전 프레임과의 거리 비교용

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 에피소드 시작마다 호출 (리셋)
    public override void OnEpisodeBegin()
    {
        // Agent 위치 랜덤 리셋
        transform.localPosition = new Vector2(
            Random.Range(-1f, -5f),
            Random.Range(-2f, 2f)
        );
        // Enemy 위치 랜덤 리셋
        enemy.localPosition = new Vector2(
            Random.Range(1f, 5f),
            Random.Range(-2f, 2f)
        );

        attackTimer = 0f;
        previousDistance = Vector2.Distance(transform.localPosition, enemy.localPosition);
    }

    // Agent가 관찰하는 값 수집
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(new Vector2(transform.localPosition.x, transform.localPosition.y));   // 내 위치 (x, y)
        sensor.AddObservation(new Vector2(enemy.localPosition.x, enemy.localPosition.y));       // 적 위치 (x, y)

        // 적과의 방향 벡터
        Vector2 dir = enemy.localPosition - transform.localPosition;
        sensor.AddObservation(dir.normalized);            // 방향 (x, y)
    }
    // 총 6개 float 값 관찰

    // 학습된 행동 실행
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

        // 공격 행동 (적과 가까울 때)
        int attack = actions.DiscreteActions[0]; // 0: 공격 안함, 1: 공격
        attackTimer -= Time.fixedDeltaTime;

        float currentDistance = Vector2.Distance(transform.localPosition, enemy.localPosition);

        // 추적 보상/패널티
        float distanceDiff = previousDistance - currentDistance;

        if (distanceDiff > 0)
            AddReward(distanceDiff * 0.05f); // 가까워지면 보상
        else
            AddReward(distanceDiff * 0.01f); // 멀어지면 패널티
        previousDistance = currentDistance;

        if (attack == 1 && attackTimer <= 0f)
        {
            attackTimer = attackCooldown;

            if (currentDistance <= attackRange)
            {
                // 공격 명중
                AddReward(1.0f);
                EndEpisode();
            }
            else
            {
                // 공격 빗나감
                AddReward(-0.01f);
            }
        }

        // 시간 패널티
        AddReward(-0.001f);
    }

    // 플레이어가 직접 조작할 때 (테스트용)
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxis("Horizontal");
        actions[1] = Input.GetAxis("Vertical");

        var disc = actionsOut.DiscreteActions;
        disc[0] = Input.GetKey(KeyCode.Space) ? 1 : 0; // 스페이스바 공격
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // 피격 패널티
            AddReward(-0.1f);
            EndEpisode();
        }
    }

    public void OnHit()
    {
        // 피격 패널티
        AddReward(-0.1f);
        EndEpisode();
    }
}
/*

### 보상 흐름 요약
```
매 스텝        시간 패널티   -0.001
적에 가까워짐  추적 보상     +거리차 × 0.01
적에 멀어짐    추적 패널티   -거리차 × 0.01
공격 명중      명중 보상     +1.0  → 에피소드 종료
공격 빗나감    낭비 패널티   -0.05
피격           피격 패널티   -0.1

*/