
using UnityEngine; 
public class RoverController : MonoBehaviour 
{ public Transform restingTarget; 
public Transform resourceTarget; 
public Transform solarTarget; 
public Transform dirtTarget; 

public float moveSpeed = 300f; 
public float turnSpeed = 2f; 
public float arrivalDistance = 30f; 
public float waitTime = 2f; 

private Transform[] route; 

private int index = 0; 

private float waitTimer = 0f; 

void Start() 

{ route = new Transform[] { resourceTarget, solarTarget, dirtTarget, restingTarget }; } 

void Update() { Transform target = route[index]; 

if (target == null) return;
Vector3 direction = target.position - transform.position; 
direction.y = 0; if (direction.magnitude <= arrivalDistance) 
{ waitTimer += Time.deltaTime; if (waitTimer >= waitTime) 
{ waitTimer = 0f; index = (index + 1) % route.Length; } return; }

 Quaternion targetRotation = Quaternion.LookRotation(direction); 
 
 transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime); 
 
 transform.position += transform.forward * moveSpeed * Time.deltaTime; } }