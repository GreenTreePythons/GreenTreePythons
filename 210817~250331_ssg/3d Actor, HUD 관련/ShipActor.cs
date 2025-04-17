public class ShipSkinPageViewActor : MonoBehaviour,
        IViewCamera,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerMoveHandler,
        IServiceLocatorComponent,
        IServiceLocatorSetupComponent
{
    [SerializeField] Camera m_Camera;
    [SerializeField] Transform m_SlotRoot;
    [SerializeField] private Transform m_RootTransform;

    [Space]
    [SerializeField] float m_RotateSpeed = 0.5f;
    [SerializeField] float m_ZoomSpeed = 2.5f;
    [SerializeField] float m_MinZoomAmount;
    [SerializeField] float m_MaxZoomAmount = 10;
    [SerializeField] bool m_UseRotateX;
    [SerializeField] bool m_UseRotateY;

    [Space]
    [SerializeField] ShipPartsType m_ResetRequestedParts;
    public ShipPartsType GetResetRequestedParts() => m_ResetRequestedParts;
    [SerializeField] ShipPartsType m_ApplyColorParts;
    public ShipPartsType GetApplyColorParts() => m_ApplyColorParts;

    bool m_IsMouseDown;
    Vector2 m_LastPointerPosition;
    GameObject m_Actor;

    ClientUserData m_UserData;
    IServiceLocator m_Service;

    public void SetupServiceLocator(IServiceLocator service)
    {
        m_UserData = service.Resolve<ClientUserData>();
        m_Service = service;
    }

    public void ShowShip()
    {   
        CameraViewActorUtility.ShowActor(ProtoDataConst.DefaultShip, m_SlotRoot);
        m_Actor = m_SlotRoot.GetChild(0).gameObject;
        m_Service.Inject(m_Actor.GetComponentInChildren<IServiceLocatorSetupComponent>());
        var shipPartsController = m_Actor.GetComponentInChildren<ShipPartsController>();
        shipPartsController.Initialize(true);
        shipPartsController.SetDeckViewActorsVisible(false);
    }

    public ShipPartsController GetShipPartsController() => m_SlotRoot.GetComponentInChildren<ShipPartsController>();

    public void SetViewCameraDepth(int depth)
    {
        m_Camera.depth = depth;
    }

    public void SetVisibleState(bool visible)
    {
        m_RootTransform.gameObject.SetActive(visible);
    }

    void Update()
    {
        float zoomAmount = Input.GetAxis("Mouse ScrollWheel") * m_ZoomSpeed * Time.deltaTime;
        if (zoomAmount != 0) Zoom(zoomAmount);
    }

    void Zoom(float zoomAmount)
    {
        var zoomTargetPosition = m_Actor.transform.position;
        Vector3 directionToTarget = zoomTargetPosition - m_Camera.transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        float newDistanceToTarget = distanceToTarget * (1 - zoomAmount);
        float clampedDistance = Mathf.Clamp(newDistanceToTarget, m_MinZoomAmount, m_MaxZoomAmount);
        float distanceRatio = clampedDistance / distanceToTarget;
        Vector3 newPosition = zoomTargetPosition - directionToTarget * distanceRatio;
        m_Camera.transform.position = newPosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_IsMouseDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        m_IsMouseDown = true;
        m_LastPointerPosition = eventData.position;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!m_IsMouseDown) return;
        var positionDelta = eventData.position - m_LastPointerPosition;
        float rotationAmountX = -positionDelta.y * m_RotateSpeed;
        float rotationAmountY = -positionDelta.x * m_RotateSpeed;
        if (m_UseRotateX) m_Actor.transform.Rotate(Vector3.right, rotationAmountX, Space.World);
        if (m_UseRotateY) m_Actor.transform.Rotate(Vector3.up, rotationAmountY, Space.World);
        m_LastPointerPosition = eventData.position;
    }
}