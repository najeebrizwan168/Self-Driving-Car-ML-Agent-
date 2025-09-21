using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using Unity.MLAgents.Actuators;
using System.Threading;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(DecisionRequester))]
public class NNtrack : Agent
{
    [System.Serializable]
    public class RewardInfo
    {
        public float nomovement = -0.1f;
        public float mult_forward = 0.001f;
        public float Barrier = -0.001f;
        public float back = -0.001f;
        public float car = -0.001f;
    }
    public float motorForce = 1500f;
    public float maxSteerAngle = 30f;
    public RewardInfo rwd = new RewardInfo();
    private Rigidbody rb = null;
    private Vector3 Recall_position;
    public bool doEpisode = true;
    private Quaternion Recall_rotation;
    private Bounds bnd;

    // Wheel Colliders (assign in Inspector)
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    public override void Initialize()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.drag = 1;
        rb.angularDrag = 5;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;
        this.GetComponent<MeshCollider>().convex = true;
        this.GetComponent<DecisionRequester>().DecisionPeriod = 1;
        bnd = this.GetComponent<MeshCollider>().bounds;
        Recall_position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
        Recall_rotation = new Quaternion(this.transform.rotation.x, this.transform.rotation.y, this.transform.rotation.z, this.transform.rotation.w); ;
    }
    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        this.transform.position = Recall_position;
        this.transform.rotation = Recall_rotation;
        ResetWheelColliders();
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!isWheelDown()) { return; }
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
        // Penalty for no movement
        if (move == 0)
        {
            AddReward(rwd.nomovement);
        }
        // Penalty for moving back
        if (move < 0)
        {
            AddReward(rwd.back);
        }
    }

    private void ApplyMovement(float move, float steer)
    {
        float steerAngle = steer * maxSteerAngle;
        float torque = move * motorForce;

        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        rearLeftWheel.motorTorque = torque;
        rearRightWheel.motorTorque = torque;
    }

    private void ResetWheelColliders()
    {
        frontLeftWheel.motorTorque = 0f;
        frontRightWheel.motorTorque = 0f;
        rearLeftWheel.motorTorque = 0f;
        rearRightWheel.motorTorque = 0f;

        frontLeftWheel.steerAngle = 0f;
        frontRightWheel.steerAngle = 0f;
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
            if (doEpisode)
            {
                EndEpisode();
            }
        }
        else if (collision.gameObject.CompareTag("Car"))
        {
            AddReward(rwd.car);
            if (doEpisode)
            {
                EndEpisode();
            }
        }
    }
    private bool isWheelDown()
    {
        return Physics.Raycast(this.transform.position, -this.transform.up, bnd.size.y * 0.55f);
    }
}