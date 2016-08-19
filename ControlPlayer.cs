using UnityEngine;

public class ControlPlayer : MonoBehaviour {
	public float tiltSensitivity;
	public float touchSensitivity;

	// Whether to pay attention to the accelerometer for input
	public bool enableAccel = true;

	/**
     * Touch Controller Variables
     */
	private float secTime;
	private float endTime;
	private Vector3   tilt        = Vector3.zero;  //The tilt expressed by the accelerometer
	private Vector3   temptilt    = Vector3.zero;  //The temporary holder value for tilt

	private Quaternion  tiltCalibrate; //the calibration level of the accelerometer
	private Touch       touch;

	private double  sensitivity       = 1.00;
	private double  tiltmod           = 5.00;

	private Vector2 startPos;
	private Vector2 cursorStartPosition;
	private Vector2 cursorCurrentPosition;
	private bool isButton = false;

	private GameConfig config;
	private PlayerEvents pEvents;
	private Transform startTarget;

	public Quaternion Calibrate() {
		return tiltCalibrate = Quaternion.FromToRotation(Input.acceleration, new Vector3(0,0,-1) );
	}

	public Quaternion GetTiltCalibrate () {
		return tiltCalibrate;
	}

	public void SetTiltCalibrate (Quaternion calibration) {
		tiltCalibrate = calibration;
	}

	public Vector3 GetTilt () {
		return tilt;
	}

	// Use this for initialization
	void Start () {
		config = GetComponent<GameConfig>();
		pEvents = GetComponent<PlayerEvents> ();
		startTarget = GameObject.FindGameObjectWithTag ("StartTarget").transform;
	
		if (config) {
			tiltSensitivity = config.TiltSensitivity;
			touchSensitivity = config.TouchSensitivity;
			tiltCalibrate = config.TiltCalibration;
		}
	}

	// Sned the player back to the start
	public void backToStart () {
		transform.position = startTarget.position;
	}

	// Update is called once per frame
	void Update () {
		//Player Controllers:
		//The mazes are presented top down, with y pointing upwards at the user.
		//x is still side to side, but z control should correspond to y axis movement
		float xval = 0;
		float yval = 0;
		tilt = Vector3.zero;

		//get directional input
		int dpmult = 4;
		// Get the input vector from keyboard or analog stick
		//gets a -1 1 or 0 for pressed (direction) or not
		tilt.z = Input.GetAxis("Vertical") * dpmult;
		tilt.x = Input.GetAxis("Horizontal") * dpmult;
	
		if (tilt != Vector3.zero)
			return;

		//give control to the player via tilt at the very beginning
		endTime = Time.fixedTime - 1;

		//touch controller:
		if (Input.touchCount > 0) {
			touch = Input.touches[0];
			switch (touch.phase) {
			case TouchPhase.Began:
				startPos = touch.position;
				break;
			case TouchPhase.Moved:
				//float touchStep = {50,90,130,150}; //the number of pixels for radius of touch areas
				break;
			case TouchPhase.Ended:
				endTime = Time.fixedTime;//this tracks when the touch control ended
				break;
			}
			xval = touch.position.y-startPos.y;//horizontal finger slide distance
			yval = touch.position.x-startPos.x;//vertical finger slide distance
			tilt.x = Mathf.Sign(yval) * Mathf.Min (Mathf.Abs(yval) / (21 - touchSensitivity), 4);
			tilt.z = Mathf.Sign(xval) * Mathf.Min (Mathf.Abs(xval) / (21 - touchSensitivity), 4);
			//end touch controller
			return;
		}

		// mouse cursor controller
		if (Input.GetKeyDown(KeyCode.Mouse0)) {
		    cursorStartPosition = Input.mousePosition;
			startPos = cursorStartPosition;
			isButton = true;
		} else if (Input.GetMouseButtonUp(0)) {
			isButton = false;
		}

		if (isButton) {
			// Set the mouse position as the touch Position for the tap handler
			cursorCurrentPosition = Input.mousePosition;

			xval = cursorCurrentPosition.y - cursorStartPosition.y;//horizontal finger slide distance
			yval = cursorCurrentPosition.x - cursorStartPosition.x;//vertical finger slide distance

			tilt.x = Mathf.Sign(yval) * Mathf.Min (Mathf.Abs(yval) / (21 - touchSensitivity), 4);
			tilt.z = Mathf.Sign(xval) * Mathf.Min (Mathf.Abs(xval) / (21 - touchSensitivity), 4);
			return;
		}

		if (enableAccel) {
			// Tilt Controller
			//.5 second before tilt takes over, unless its the level start)
			if (tilt == Vector3.zero && Time.fixedTime - endTime > .5f) {
				//if there's no touch or directional control, use the accelerometer:
				temptilt = Input.acceleration;
				if (temptilt != Vector3.zero) {
					//apply the calibration values to the tilt
					temptilt = tiltCalibrate * temptilt;
					tilt.x = (float)Mathf.Sign (temptilt.x) * Mathf.Min ((float)(Mathf.Abs (temptilt.x) * (tiltmod + tiltSensitivity)), 4.0f);
					tilt.z = (float)Mathf.Sign (temptilt.y) * Mathf.Min ((float)(Mathf.Abs (temptilt.y) * (tiltmod + tiltSensitivity)), 4.0f);
				}
			}
		}
	}

	void FixedUpdate()
	{
		//Apply the control force to the player's ball
		pEvents.controlBall(tilt, (float) sensitivity * Time.fixedDeltaTime * 50);
	}
}
