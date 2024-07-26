using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera[] _VirtualCameras;
    public static CameraManager instance;

    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallYPanTime = 0.35f;
    [SerializeField] private float fallYOffset = 0.5f;
    [SerializeField] private float fallYOffsetTime = 0.5f;
    public float fallSpeedYDampingThreshold = -15f;     // Speed the player must fall at before YDamping changes
    [SerializeField] private float wallJumpPanAmount = 3f;
    [SerializeField] private float jumpXPanTime = 0.35f;
    public float wallJumpXDampingThreshold = 2f;        // Times the player must wallJump in succession before XDamping changes

    public bool IsLerpingYDamping { get ; private set; }
    public bool IsLerpingXDamping { get ; private set; }
    public bool LerpedFromPlayerFalling { get; set;}
    public bool LerpedFromPlayerWallJumping { get; set;}

    private CinemachineVirtualCamera currentCamera;
    private CinemachineFramingTransposer framingTransposer;

    private float defaultYPanAmount;
    private float defaultXPanAmount;
    private float defaultYOffset;

    void Awake()
    {
        if (instance == null) instance = this;

        for (int i = 0; i < _VirtualCameras.Length; i++)
        {
            if (_VirtualCameras[i].enabled)
            {
                currentCamera = _VirtualCameras[i];
                framingTransposer = currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            }
        }

        defaultYPanAmount = framingTransposer.m_YDamping;
        defaultYOffset = framingTransposer.m_ScreenY;
        defaultXPanAmount = framingTransposer.m_XDamping;
    }

    public void LerpYDamping(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;

        float startDampAmount = framingTransposer.m_YDamping;
        float endDampAmount = defaultYPanAmount;

        float startYOffset = framingTransposer.m_ScreenY;
        float endYOffset = defaultYOffset;

        if (isPlayerFalling)
        {
            LerpedFromPlayerFalling = true;
            endDampAmount = fallPanAmount;
            endYOffset = fallYOffset;
        }

        DOVirtual.Float(startDampAmount, endDampAmount, fallYPanTime, ChangeYDampingValue);
        DOVirtual.Float(startYOffset, endYOffset, fallYOffsetTime, ChangeYOffset);

        IsLerpingYDamping = false;
    }

    public void LerpXDamping(bool isPlayerWallJumping)
    {
        IsLerpingXDamping = true;

        float startDampAmount = framingTransposer.m_XDamping;
        float endDampAmount = defaultXPanAmount;

        if (isPlayerWallJumping)
        {
            LerpedFromPlayerWallJumping = true;
            endDampAmount = wallJumpPanAmount;
        }

        DOVirtual.Float(startDampAmount, endDampAmount, jumpXPanTime, ChangeXDampingValue);

        IsLerpingXDamping = false;
    }

    private void ChangeYDampingValue(float value)
    {
        framingTransposer.m_YDamping = value;
    }

    private void ChangeXDampingValue(float value)
    {
        framingTransposer.m_XDamping = value;
    }

    private void ChangeYOffset(float value)
    {
        framingTransposer.m_ScreenY = value;
    }
}
