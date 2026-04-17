using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using rinCore;
using Unity.Cinemachine;
public class Kar : MonoBehaviour
{
    [SerializeField] InputActionReference moveAction, brakeAction;
    [SerializeField] List<WheelCollider> forwardWheels = new(), steeringWheels = new();
    [SerializeField, Tooltip("Default 45")] float angleMultiplier = 45f;
    [SerializeField, Tooltip("Default 800")] float forceMultiplier = 800f;
    [SerializeField, Tooltip("Default 4000")] float brakeForce = 4000f;
    [SerializeField, Tooltip("Max forward speed")] float maxSpeed = 25f;
    [SerializeField] CinemachineCamera _cam;
    [SerializeField] Rigidbody _speedbody;

    private void Update()
    {
        RunCar();
    }
    void RunCar()
    {
        Vector2 input = moveAction.ReadRawVector2();
        if (input.x.Absolute() < 0.2f)
            input.x = 0f;

        Vector3 camForward = _cam.transform.forward;
        Vector3 camRight = _cam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = (camForward * input.y + camRight * input.x).normalized;

        Vector3 velocity = _speedbody.linearVelocity;
        float speed = velocity.magnitude;

        bool hasInput = input.sqrMagnitude > 0.0001f;
        bool isBraking = brakeAction.IsPressedRaw();

        float cameraTarget = 80f;

        float alignment = speed > 0.1f ? Vector3.Dot(velocity / speed, inputDir) : 0f;
        float aligned = Mathf.InverseLerp(0.3f, 0.7f, alignment);

        if (hasInput && speed > 2f)
        {
            cameraTarget = Mathf.Lerp(80f, (speed * 2.5f + 80f).Clamp(70f, 105f), aligned);
        }

        _cam.Lens.FieldOfView =
            _cam.Lens.FieldOfView.MoveTowards(cameraTarget, 10f * Time.deltaTime);

        if (isBraking)
        {
            foreach (var wheel in forwardWheels)
            {
                wheel.motorTorque = 0f;
                wheel.brakeTorque = brakeForce;
            }

            foreach (var wheel in steeringWheels)
            {
                wheel.steerAngle = wheel.steerAngle.MoveTowards(input.x * angleMultiplier, angleMultiplier * Time.deltaTime);
            }

            return;
        }

        if (!hasInput)
        {
            foreach (var wheel in forwardWheels)
            {
                wheel.motorTorque = 0f;
                wheel.brakeTorque = 0f;
            }

            foreach (var wheel in steeringWheels)
            {
                wheel.steerAngle = wheel.steerAngle.MoveTowards(0f, angleMultiplier * 3f * Time.deltaTime);
            }

            return;
        }

        foreach (var wheel in forwardWheels)
        {
            wheel.brakeTorque = 0f;

            float speedFactor = 1f - Mathf.Clamp01(speed / maxSpeed);
            speedFactor *= speedFactor;

            float directional = speed > 0.1f ? Vector3.Dot(velocity.normalized, transform.forward) : 0f;
            float sameDirFactor = Mathf.Clamp01(1f - Mathf.Abs(directional) * (speed / maxSpeed));

            wheel.motorTorque =
                forceMultiplier * input.y * speedFactor * sameDirFactor;
        }

        foreach (var wheel in steeringWheels)
        {
            wheel.steerAngle =
                wheel.steerAngle.MoveTowards(input.x * angleMultiplier, angleMultiplier * Time.deltaTime);
        }
    }
}
