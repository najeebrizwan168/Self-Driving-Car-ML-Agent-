using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using Unity.MLAgents.Actuators;
using System.Threading;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DecisionRequester))]
public class NNArea : Agent
{
    [System.Serializable]
    public class RewardInfo
    {
        public float mult_forward = 0.001f;
        public float Barrier = -0.001f;
        public float car = -0.001f;
    }
    public float motorForce = 1500f;
    public float maxSteerAngle = 30f;
    public RewardInfo rwd = new RewardInfo();

    // Assign these in inspector
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    private Rigidbody rb = null;
    private Vector3 Recall_position;
    private Quaternion Recall_rotation;

    public override void Initialize()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.drag = 1;
        rb.angularDrag = 5;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;
        this.GetComponent<DecisionRequester>().DecisionPeriod = 1;
        Recall_position = transform.position;
        Recall_rotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = Recall_position;
        transform.rotation = Recall_rotation;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float move = 0f;
        float steer = 0f;
        switch (actions.DiscreteActions.Array[0])
        {
            case 1: move = -1f; break; // Back
            case 2: move = 1f; break;  // Forward
        }
        switch (actions.DiscreteActions.Array[1])
        {
            case 1: steer = -1f; break; // Left
            case 2: steer = 1f; break;  // Right
        }

        ApplyMovement(move, steer);

        // Reward for moving forward
        if (move > 0)
        {
            AddReward(rb.velocity.magnitude * rwd.mult_forward);
        }
    }

    private void ApplyMovement(float move, float steer)
    {
        float steerAngle = steer * maxSteerAngle;
        float torque = move * motorForce;

        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        // Apply torque to rear wheels (common for FWD; adjust for AWD/RWD as needed)
        rearLeftWheel.motorTorque = torque;
        rearRightWheel.motorTorque = torque;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        actionsOut.DiscreteActions.Array[0] = 0;
        actionsOut.DiscreteActions.Array[1] = 0;
        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");
        if (move < 0)
            actionsOut.DiscreteActions.Array[0] = 1;
        else if (move > 0)
            actionsOut.DiscreteActions.Array[0] = 2;
        if (turn < 0)
            actionsOut.DiscreteActions.Array[1] = 1;
        else if (turn > 0)
            actionsOut.DiscreteActions.Array[1] = 2;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("BarrierLeft") || collision.gameObject.CompareTag("BarrierRight"))
        {
            AddReward(rwd.Barrier);
        }
        else if (collision.gameObject.CompareTag("Car"))
        {
            AddReward(rwd.car);
        }
    }
}