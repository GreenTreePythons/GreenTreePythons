using System;
using System.Collections;
using UnityEngine;
using Latecia.Shared;
using MagicaCloth2;
using SingleSquadBattle;
using ViewSystem;
using UnityEngine.EventSystems;

public class CharacterPageViewActor : MonoBehaviour, 
    IViewCamera,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerMoveHandler,
    IDragHandler
{
    const int STAND1_LOOP_COUNT = 9;
    const int STAND2_LOOP_COUNT = 1;
    const float DEFAULT_SIZE = 1.2f;
    
    public enum LobbyParticle
    {
        Awakening = 0,
        Evolution = 1,
        EvolutionReady = 2,
        LevelUp = 3,
        Transcendence = 4,
    }

    public enum CameraPosition
    {
        Normal = 0,
        Detail = 1,
        Awakening = 2,
    }
    
    [SerializeField] Camera m_CharacterCamera;

    [Header("Particle")]
    [SerializeField] ParticleSystem[] m_LobbyParticles;
    [SerializeField] ParticleSystem[] m_ChangeParticles;

    [Header("CameraPositions")] 
    [SerializeField, Range(0.5f, 10f)] float m_ReferenceCharacterSize = 1.2f;
    [SerializeField] Transform[] m_CameraTransforms;
    [SerializeField] Transform m_NormalCameraRoot;
    [SerializeField] Transform m_DetailCameraRoot;
    [SerializeField] Transform m_AwakeningCameraRoot;
    [SerializeField] AnimationCurve m_CameraTransition;
    
    [Space]
    [SerializeField] GameObject m_FloorRoot;
    [SerializeField] GameObject[] m_Floors;
    [SerializeField] GameObject m_Root;
    [SerializeField] Transform m_SlotTransform;

    [Space]
    [SerializeField] float m_RotateSpeed = 0.5f;
    [SerializeField] float m_ZoomSpeed = 2.5f;
    [SerializeField] float m_MinZoomAmount;
    [SerializeField] float m_MaxZoomAmount = 10;

    [SerializeField] UIAnimation m_FocusAnim;

    CharacterProto m_Proto;
    CharacterActor m_Actor;
    Coroutine m_CameraAnimationCoroutine;
    Coroutine m_StandAnimationCoroutine;
    ParticleSystem m_Particle;
    float m_Stand1ClipLength;
    float m_Stand2ClipLength;
    float m_ReactionClipLength;
    float m_SpawnClipLength;

    private CameraPosition m_CameraPosition;
    float[] m_CharacterCameraOriginX;
    public float GetCharacterCameraOriginX(int index) => m_CharacterCameraOriginX[index];
    Transform m_AwakeningHeadTransform;
    Transform m_HeadTransform;     
    Transform m_ZoomPoint;
    float m_DefaultHeight;

    bool m_IsMouseDown;
    bool m_IsDragging;
    Vector2 m_LastPointerPosition;

    bool m_IsInitialize = false;
    Coroutine m_RotCoroutine = null;

    private void Start() => Initialize();

    void Initialize()
    {
        if(m_IsInitialize) return;
        
        var targetTransform = m_CameraTransforms[0];
        m_CharacterCamera.transform.position = targetTransform.position;
        m_CharacterCamera.transform.rotation = targetTransform.rotation;
        m_CharacterCameraOriginX = new float[3];
        var index = 0;
        foreach(var camTransform in m_CameraTransforms)
        {
            m_CharacterCameraOriginX[index] = camTransform.position.x;
            index++;
        }
        m_IsInitialize = true;
    }

    private void SetFloor(CharacterElement element)
    {
        var floorObject = element switch
        {
            CharacterElement.Fire => m_Floors[0],
            CharacterElement.Water => m_Floors[1],
            CharacterElement.Tree => m_Floors[2],
            CharacterElement.Dark => m_Floors[3],
            CharacterElement.Light => m_Floors[4],
            _ => default
        };

        foreach (var floor in m_Floors)
        {
            floor.SetActive(floor == floorObject);
        }
    }
    
    private void ShowChangeEffect(CharacterElement element)
    {
        var changeParticle = element switch
        {
            CharacterElement.Fire => m_ChangeParticles[0],
            CharacterElement.Water => m_ChangeParticles[1],
            CharacterElement.Tree => m_ChangeParticles[2],
            CharacterElement.Dark => m_ChangeParticles[3],
            CharacterElement.Light => m_ChangeParticles[4],
            _ => default
        };

        m_FloorRoot.SetActive(true);
        
        PlayParticle(changeParticle);
    }

    public void SetCameraPosition(CameraPosition cameraPosition, bool instant)
    {
        if(!m_IsInitialize) Initialize();

        if (m_CameraAnimationCoroutine != null)
        {
            StopCoroutine(m_CameraAnimationCoroutine);
        }
        
        m_CameraPosition = cameraPosition;
        var targetTransform = m_CameraTransforms[(int)m_CameraPosition];
        var adjustmentTransform = cameraPosition == CameraPosition.Awakening ? m_AwakeningHeadTransform : m_HeadTransform;
        float adjustmentPosX = adjustmentTransform != null ? adjustmentTransform.position.x : 0.0f;
        targetTransform.position = new Vector3(m_CharacterCameraOriginX[(int)m_CameraPosition] + adjustmentPosX,
                                    targetTransform.position.y,
                                    targetTransform.position.z);

        m_CameraAnimationCoroutine = StartCoroutine(_CoMoveCamera());

        IEnumerator _CoMoveCamera()
        {
            if (!instant)
            {
                var fromPos = m_CharacterCamera.transform.position;
                var fromRot = m_CharacterCamera.transform.rotation;
                var time = m_CameraTransition.keys[^1].time;
                
                for (var f = 0f; f < time; f += Time.deltaTime)
                {
                    var lerpValue = m_CameraTransition.Evaluate(f);
                    m_CharacterCamera.transform.position = Vector3.Lerp(fromPos, targetTransform.position, lerpValue);
                    m_CharacterCamera.transform.rotation = Quaternion.Slerp(fromRot, targetTransform.rotation, lerpValue);
                    yield return null;
                }
            }
            
            m_CharacterCamera.transform.position = targetTransform.position;
            m_CharacterCamera.transform.rotation = targetTransform.rotation;
            m_CameraAnimationCoroutine = null;
        }
    }

    private void Update()
    {
        float zoomAmount = Input.GetAxis("Mouse ScrollWheel") * m_ZoomSpeed * Time.deltaTime;
        if (zoomAmount != 0) Zoom(zoomAmount);

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Return))
        {
            var adjustmentTransform = m_CameraPosition == CameraPosition.Awakening ? m_AwakeningHeadTransform : m_HeadTransform;
            float adjustmentPosX = adjustmentTransform != null ? adjustmentTransform.position.x : 0.0f;
            if(adjustmentTransform == null)
            {
                SharedDebug.Log($"cam type({m_CameraPosition}) , adjustmentPosValue : null");
                return;
            }
            SharedDebug.Log($"cam type({m_CameraPosition}) , adjustmentPosValue : {adjustmentTransform.position}");

            m_CharacterCamera.transform.position = new Vector3(m_CharacterCameraOriginX[(int)m_CameraPosition] + adjustmentPosX,
                                        m_CharacterCamera.transform.position.y,
                                        m_CharacterCamera.transform.position.z);
        }
#endif
    }

    public void PlayParticle(LobbyParticle particle)
    {
        var effect = m_LobbyParticles[(int)particle];
        PlayParticle(effect);
    }

    public void PlayParticle(ParticleSystem particle)
    {
        if(m_Particle != null)
        {
            Destroy(m_Particle.gameObject);
            m_Particle = null;
        }
        
        if (particle == null) return;
        
        m_Particle = Instantiate(particle, m_SlotTransform);
        m_Particle.gameObject.SetActive(true);
        m_Particle.Play();
    }

    public void ShowCharacter(CharacterData data, string initAnim = null)
    {
        ShowCharacter(data.GetProto(), initAnim);
    }

    public void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        var cameraPos = m_CharacterCamera.transform.position;
        var distance = cameraPos - m_HeadTransform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraPos, m_HeadTransform.position);

        Gizmos.color = Color.blue;
        var forwardVec = new Vector3(cameraPos.x, cameraPos.y, cameraPos.z - distance.z);
        Gizmos.DrawLine(cameraPos, forwardVec);
    }

    public void UpdateCameraPosition()
    {
        var battleActor = m_Actor.GetComponent<ClientBattleActor>();
        if (m_HeadTransform != null)
        {
            var diff = m_HeadTransform.position - battleActor.transform.position;
            var ratio = diff.y / m_ReferenceCharacterSize;
            m_NormalCameraRoot.localScale = Vector3.one * ratio;
            m_AwakeningCameraRoot.localScale = Vector3.one * ratio;
        }
    }

    public void ShowCharacter(CharacterProto proto, string initAnim = null)
    {
        if(m_Proto == proto) return;
        
        if (SceneBattleManager.Instance != null) SceneBattleManager.Instance.SetActiveBattleSceneLight(false);
        if (SpaceSceneManager.Instance != null) SpaceSceneManager.Instance.SetActiveSpaceSceneLight(false);

        if (m_Actor != null)
        {
            Destroy(m_Actor.gameObject);
        }
        
        m_Proto = proto;
        var inst = BundleUtility.InstantiateAsync(proto.HighModelPrefab).Wait();
        m_Actor = inst.AddComponent<CharacterActor>();
        foreach (var b in m_Actor.GetComponentsInChildren<ClothBehaviour>(true)) b.enabled = true;
        
        m_Actor.transform.SetParent(m_SlotTransform);
        m_Actor.transform.localPosition = Vector3.zero;
        m_Actor.transform.localRotation = Quaternion.identity;
        m_Actor.transform.localScale = Vector3.one;
        m_Actor.gameObject.SetLayer(LayerUtility.CHARACTER, true);
        
        ShowChangeEffect(proto.Element);
        SetFloor(proto.Element);

        var battleActor = m_Actor.GetComponent<ClientBattleActor>();
        if (battleActor == null || battleActor.Animator == null)
        {
            if (m_StandAnimationCoroutine != null) 
            {
                StopCoroutine(m_StandAnimationCoroutine);
                m_StandAnimationCoroutine = null;
            }
            return;
        }

        m_Actor.transform.localPosition = Vector3.zero - Vector3.up * battleActor.DefaultHeight;
        m_NormalCameraRoot.localScale = Vector3.one;

        m_AwakeningHeadTransform = battleActor.GetAwakeningCameraTarget();
        m_HeadTransform = battleActor.GetCameraTarget();
        m_ZoomPoint = battleActor.GetCameraZoomPoint();
        m_DefaultHeight = battleActor.DefaultHeight;
        battleActor.enabled = false;
        UpdateCameraPosition();

        m_DetailCameraRoot.localPosition = Vector3.up * m_ReferenceCharacterSize;
        var zoomTarget = battleActor.GetCameraZoomPoint();
        if (zoomTarget != null)
        {
            m_DetailCameraRoot.position = zoomTarget.position;
        }
        
        // get clip's length
        m_Stand1ClipLength = _GetLength(BattleAnim.Stand1.Value, 0.5f);
        m_Stand2ClipLength = _GetLength(BattleAnim.Stand2.Value, 0.5f);
        m_SpawnClipLength = _GetLength(BattleAnim.Spawn.Value, 0.5f);
        m_ReactionClipLength = _GetLength(BattleAnim.Reaction.Value, 0.5f);

        if (m_StandAnimationCoroutine != null) StopCoroutine(m_StandAnimationCoroutine);
        if (initAnim != null) m_StandAnimationCoroutine = StartCoroutine(CoPlayAnimation(initAnim));
        else m_StandAnimationCoroutine = StartCoroutine(CoPlayAnimation(BattleAnim.Spawn.Value));
        battleActor.Animator.BaseLayer.Play(BattleAnim.Stand1.Value, updateImmediate: true);

        var cam = battleActor.GetComponentInChildren<Camera>();
        if (cam != null) cam.gameObject.SetActive(false);

        float _GetLength(string animationName, float defaultValue)
        {
            return battleActor.Animator.TryFindAnimationData(animationName, out var data) ? data.ClipLength : defaultValue;
        }
    }

    public void PlayReaction(bool isDragging)
    {
        if (isDragging) return;
        if (m_CameraPosition == CameraPosition.Detail) return;
        if (m_Actor.GetComponent<ClientBattleActor>().Animator.DefaultLayer.IsPlaying(BattleAnim.Reaction.Value)) return;
        if (m_StandAnimationCoroutine != null) StopCoroutine(m_StandAnimationCoroutine);
        m_StandAnimationCoroutine = StartCoroutine(CoPlayAnimation(BattleAnim.Reaction.Value));
    }

    IEnumerator CoPlayAnimation(string anim)
    {
        if (!(anim == BattleAnim.Reaction.Value || anim == BattleAnim.Spawn.Value)) yield break;
        m_Actor.GetComponent<ClientBattleActor>().Animator.DefaultLayer.CrossFade(anim, clamped: true);
        var waitTime = anim == BattleAnim.Reaction.Value ? m_ReactionClipLength : m_SpawnClipLength;
        yield return new WaitForSeconds(waitTime);

        while (true)
        {
            m_Actor.GetComponent<ClientBattleActor>().Animator.DefaultLayer.CrossFade(BattleAnim.Stand1.Value, clamped: true);
            yield return new WaitForSeconds(m_Stand1ClipLength * STAND1_LOOP_COUNT);
            m_Actor.GetComponent<ClientBattleActor>().Animator.DefaultLayer.CrossFade(BattleAnim.Stand2.Value, clamped: true);
            yield return new WaitForSeconds(m_Stand2ClipLength * STAND2_LOOP_COUNT);
        }
    }
    
    void IViewCamera.SetViewCameraDepth(int depth)
    {
        m_CharacterCamera.depth = depth;
    }

    void IViewCamera.SetVisibleState(bool visible)
    {
        m_Root.SetActive(visible);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        m_IsMouseDown = true;
        m_IsDragging = false;
        m_LastPointerPosition = eventData.position; 
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_IsMouseDown = false;
        PlayReaction(m_IsDragging);
        m_IsDragging = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if(!m_IsMouseDown || m_CameraPosition != CameraPosition.Normal) return;

        var positionDelta = eventData.position - m_LastPointerPosition;
        var viewportMultiplier = 1920f / Screen.width;
        float rotationAmount = -positionDelta.x * viewportMultiplier * m_RotateSpeed;
        m_Actor.transform.Rotate(Vector3.up, rotationAmount, Space.World);
        m_LastPointerPosition = eventData.position;

        if(m_RotCoroutine != null) StopCoroutine(m_RotCoroutine);
        m_RotCoroutine = StartCoroutine(CoRotateCharacter(2.28f, 1.0f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        m_IsDragging = eventData.dragging;
    }

    public void InitializeCharacterRot(float rotTime = 1.0f)
    {
        if(m_Actor.transform.localRotation != Quaternion.identity)
        {
            StartCoroutine(CoInitializeCharacterRot(rotTime));
        }
    }

    IEnumerator CoInitializeCharacterRot(float rotTime)
    {
        float elapsedTime = 0f;
        Quaternion initialRotation = m_Actor.transform.localRotation;
        Quaternion targetRotation = Quaternion.identity;
        while (elapsedTime < rotTime)
        {
            m_Actor.transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / rotTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        m_Actor.transform.localRotation = targetRotation;
    }

    IEnumerator CoRotateCharacter(float rotTime, float initRotTime)
    {
        float elapsedTime = 0f;
        while (elapsedTime < rotTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield return StartCoroutine(CoInitializeCharacterRot(initRotTime));
        m_RotCoroutine = null;
    }

    void Zoom(float zoomAmount)
    {
        var zoomTargetPosition = m_ZoomPoint.position;
        Vector3 directionToTarget = zoomTargetPosition - m_CharacterCamera.transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        float newDistanceToTarget = distanceToTarget * (1 - zoomAmount);
        float clampedDistance = Mathf.Clamp(newDistanceToTarget, m_MinZoomAmount, m_MaxZoomAmount);
        float distanceRatio = clampedDistance / distanceToTarget;
        Vector3 newPosition = zoomTargetPosition - directionToTarget * distanceRatio;
        m_CharacterCamera.transform.position = newPosition;
    }

    void OnDisable()
    {
        if (SceneBattleManager.Instance != null) SceneBattleManager.Instance.SetActiveBattleSceneLight(true);
        if (SpaceSceneManager.Instance != null) SpaceSceneManager.Instance.SetActiveSpaceSceneLight(true);
    }

    public Camera GetCamera() => m_CharacterCamera;

    public void InitializeAwakeningFocusIn()
    {
        m_FocusAnim.enabled = false;
        m_FocusAnim.AutoPlay = false;
        m_FocusAnim.SampleProgress(0);
    }

    public void PlayFocusIn()
    {
        m_FocusAnim.enabled = true;
        m_FocusAnim.SampleProgress(0);
        m_FocusAnim.ShowAnimation();
    }

    public void SetSlotPositionX(float posX)
    {
        m_SlotTransform.position = new Vector3(posX, m_SlotTransform.position.y, m_SlotTransform.position.z);
    }
}