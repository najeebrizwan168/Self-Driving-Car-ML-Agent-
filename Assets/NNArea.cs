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
public class NNArea : Agent
{
    [System.Serializable]
    public class RewardInfo
    {
        public float mult_forward = 0.001f;
        public float Barrier = -0.001f;
        public float car = -0.001f;
    }
    public float Movespeed = 30f;
    public float Turnspeed = 100f;
    public RewardInfo rwd = new RewardInfo();
    private Rigidbody rb = null;
    private Vector3 Recall_position;
    private Quaternion Recall_rotation;
    private Bounds bnd;

    public override void Initialize()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.drag = 1;
        rb.angularDrag = 5;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;
        this.GetComponent<MeshCollider>().convex= true;
        this.GetComponent<DecisionRequester>().DecisionPeriod = 1;
        bnd = this.GetComponent<MeshCollider>().bounds;
        Recall_position=new Vector3 (this.transform.position.x,this.transform.position.y,this.transform.position.z);
        Recall_rotation = new Quaternion(this.transform.rotation.x, this.transform.rotation.y, this.transform.rotation.z, this.transform.rotation.w); ;
    }
    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        this.transform.position = Recall_position;
        this.transform.rotation = Recall_rotation;
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isWheelDown() == false) { return; }
        float mag = rb.velocity.sqrMagnitude;

        switch (actions.DiscreteActions.Array[0])
        {
            case 0:
                break;
                case 1:
                rb.AddRelativeForce(Vector3.back*Movespeed*Time.deltaTime,ForceMode.VelocityChange); 
                break;
                case 2:
                rb.AddRelativeForce(Vector3.forward*Movespeed*Time.deltaTime, ForceMode.VelocityChange);
                AddReward(mag * rwd.mult_forward);
                break;
        }
        switch (actions.DiscreteActions.Array[1])
        {
            case 0:
                break;
                case 1:
                this.transform.Rotate(Vector3.up,-Turnspeed*Time.deltaTime);
                break;
                case 2:
                this.transform.Rotate(Vector3.up,Turnspeed*Time.deltaTime);
                break;
        }
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        actionsOut.DiscreteActions.Array[0] = 0;
        actionsOut.DiscreteActions.Array[1]= 0;
        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");
        if (move < 0)
        {
            actionsOut.DiscreteActions.Array[0] = 1;
        }
        else if (move > 0)
        {
            actionsOut.DiscreteActions.Array[0] = 2;
        }
        if(turn < 0)
        {
            actionsOut.DiscreteActions.Array[1] = 1;
        }
        else if(turn > 0)
        {
            actionsOut.DiscreteActions.Array[1] = 2;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Barrier") == true)
        {
            AddReward(rwd.Barrier);
        }
        else if(collision.gameObject.CompareTag("Car")==true)
        {
            AddReward(rwd.car);
        }
    }
    private bool isWheelDown()
    {
        return Physics.Raycast(this.transform.position, -this.transform.up, bnd.size.y * 0.55f);
    }
}
