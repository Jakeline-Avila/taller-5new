var waterLevel: float;
var floatHeight: float;
var buoyancyCentreOffset: Vector3;
var bounceDamp: float;



function FixedUpdate () {
	var actionPoint = transform.position + transform.TransformDirection(buoyancyCentreOffset);
	var forceFactor = 1f - ((actionPoint.y - waterLevel) / floatHeight);
	
	if (forceFactor > 0f) {
		var uplift = -Physics.gravity * (forceFactor - rigidbody.velocity.y * bounceDamp);
		rigidbody.AddForceAtPosition(uplift, actionPoint);
	}
}

