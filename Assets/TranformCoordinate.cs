using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;

#region Data Structure
[Serializable]
public class Node
{
	public double[] node = new double[3];

	public Node(double _x, double _y, double _z)
	{
		node[0] = _x;
		node[1] = _y;
		node[2] = _z;
	}
}

[Serializable]
public class Polygon
{
	public List<Node> polygon;
}

[Serializable]
public class Geometry
{
	public string type;
	public List<Polygon> coordinates;
}

public class Properties
{
	public string buildingName;
	public float height;
	public string idb;
	public string id;
}

public class Feature
{
	public string type;
	public Geometry geometry;
	public Properties properties;
}

public class GeoJson
{
	public string type;
	public Feature[] features;
}
#endregion

public class TranformCoordinate : MonoBehaviour
{
	public enum Mode
	{
		ConvertAll, CloneFromBaseObject
	};
	[Header("Mode")]
	public Mode mode;
	[Header("Convert all verticle's object to long-lat value")]
	#region Tools
	public float scale = 0.02647058823f;// Ben Thanh 90m/3400unit
	public string strIndex;
	public int[] listIndex;
	public int fromIndex, toIndex;
	public double verticalDistance, horizontalDistance, distance;
	public string fileName;
	Geometry geometry = new Geometry
	{
		type = "Polygon",
		coordinates = new List<Polygon>()
	};
	#endregion

	#region Test
	//public Vector2 location1, location2; //x = latitude, y = longtitude
	//public float distance = 0;
	Node testVector;
	//public double norVectorX = 10.67191101817744;
	//public double norVectorY = 106.69739904020867;
	#endregion

	#region verticles
	public GameObject verticelsPoint;
	public Transform parentPoint;
	Vector3[] vertices;
	Vector3[] wVerticles;
	[SerializeField]
	Mesh mesh;
	#endregion

	#region Location
	public bool baseFromOtherLocation = true;
	//public Vector2 mediate;
	//public Vector2 normal;
	//public double alphaFromNorth;
	Vector3 baseWPosition;
	public float EarthRadius = 6378.1f; //#Radius of the Earth m
	public double yGround = 0;
	public Node[] listGPSLocation;
	public Node defaultGPSLocation = new Node(10.772985184831029, 0.00521848852, 106.6981250024237); //Ben Thanh y-km
																																																	 //public Node defaultGPSLocation = new Node(10.776454755139342, 0.0125631501145227, 106.70312572937577); //Nha hat
																																																	 //Vector3 prevPoint;
	bool isFirstLocation = true;
	#endregion

	[Header("Create multi object from base object")]
	List<double> cloneAngles = new List<double>(); //heading
	List<double> cloneDistances = new List<double>(); //distanceKm
	GameObject baseMeshObject;
	GameObject baseObject;

	//[Header("Calculate Symmetric location")]
	//public Node[] listLocation1, listLocation2;
	//public Transform point1, point2;
	//public double distanceToSymetricLocation;

	void Start()
	{
		baseWPosition = new Vector3(-2932.167f, 197.2427f, 4816.456f);
		scale = 0.02647058823f;
		defaultGPSLocation = new Node(10.773070955337595, 0.00521848852, 106.69810670721084);
		//mediate = new Vector2(4397, 3476);
		//normal = new Vector2(-365.912f, -365.375f);
		SaveSystem.Init();
		if (mode == Mode.ConvertAll)
		{
			mesh = GetComponent<MeshFilter>().mesh;
		}
		else
		{
			mesh = baseMeshObject.GetComponent<MeshFilter>().mesh;
			SaveCloneProperties();
		}
		vertices = mesh.vertices;
		int i = 0;
		if (!baseFromOtherLocation)
		{
			wVerticles = new Vector3[vertices.Length];
		}
		else
		{
			wVerticles = new Vector3[vertices.Length + 1];
			wVerticles[0] = baseWPosition;
			i = 1;
		}
		foreach (Vector3 verticle in vertices)
		{
			Vector3 worldPt;
			if (mode == Mode.CloneFromBaseObject)
			{
				worldPt = baseMeshObject.transform.TransformPoint(verticle);
			}
			else worldPt = transform.TransformPoint(verticle);

			//Debug.Log(worldPt.y);

			bool isValid = true;
			foreach (Vector3 wVerticle in wVerticles)
			{
				if (CompareNode(wVerticle, worldPt))
				{
					isValid = false;
					break;
				}
			}

			if (isValid)
			{
				GameObject nodeObject = Instantiate(verticelsPoint, worldPt, Quaternion.identity, parentPoint);
				nodeObject.name += "_" + (i + 1);
				wVerticles[i] = worldPt;
				i++;
			}
		}
		listGPSLocation = new Node[i];

		listGPSLocation[0] = defaultGPSLocation;

		ConvertAllVerticlesToGPSLocation();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.S)) Save();

		if (Input.GetKeyDown(KeyCode.G))
		{
			string[] newString = strIndex.Split('.');
			listIndex = new int[newString.Length];
			for (int iStr = 0; iStr < listIndex.Length; iStr++)
			{
				listIndex[iStr] = Int32.Parse(newString[iStr]);
			}
		}

		if (Input.GetKeyDown(KeyCode.C))
		{
			verticalDistance = (wVerticles[toIndex - 1].y - wVerticles[fromIndex - 1].y) * scale;
			horizontalDistance = Math.Sqrt(Math.Pow(wVerticles[toIndex - 1].x - wVerticles[fromIndex - 1].x, 2) + Math.Pow(wVerticles[toIndex - 1].z - wVerticles[fromIndex - 1].z, 2)) * scale;
			distance = Math.Sqrt(Math.Pow(wVerticles[toIndex - 1].x - wVerticles[fromIndex - 1].x, 2)
													 + Math.Pow(wVerticles[toIndex - 1].y - wVerticles[fromIndex - 1].y, 2)
													 + Math.Pow(wVerticles[toIndex - 1].z - wVerticles[fromIndex - 1].z, 2)) * scale;
		}

		//if (Input.GetKeyDown(KeyCode.M))
		//{
		//    SaveSymetricLocation();
		//}
	}

	public void Save()
	{
		//geometry.coordinates = new List<Polygon>();
		//Polygon polygon = new Polygon()
		//{
		//    polygon = new List<Node>()
		//};
		string str = "[";
		string[] cloneStr = new string[cloneAngles.Count + 1];
		cloneStr[0] = "[";

		int i = 0;

		foreach (int index in listIndex)
		{
			i++;
			//Node baseSymetric;

			double newY = listGPSLocation[index - 1].node[1] * 1000;

			str += "[" + listGPSLocation[index - 1].node[2] + "," + listGPSLocation[index - 1].node[0] + "," + newY + "]";
			if (i == listIndex.Length) str += "]";
			else str += ",";

			cloneStr[0] += "[" + listGPSLocation[index - 1].node[2] + "," + listGPSLocation[index - 1].node[0] + "," + newY + "]";
			if (i == listIndex.Length) cloneStr[0] += "]";
			else cloneStr[0] += ",";

			//Vector2 hypotenuse = new Vector2(wVerticles[index - 1].x - mediate.x, wVerticles[index - 1].z - mediate.y);
			//float alpha = Vector2.Angle(hypotenuse, normal);
			//Vector2 wVerticalV2 = new Vector2(wVerticles[index - 1].x, wVerticles[index - 1].z);
			//double distanceTosymmetricLocation = (Math.Sin(ToRadians(alpha)) * Vector2.Distance(wVerticalV2, mediate)) * 2;

			//Node cloneFirstLocation = ConvertVeticle(listGPSLocation[index - 1].node[0], listGPSLocation[index - 1].node[2], alphaFromNorth, distanceTosymmetricLocation * scale / 1000);
			//Node firstLocation = new Node(cloneFirstLocation.node[0], listGPSLocation[index - 1].node[1], cloneFirstLocation.node[2]);
			//cloneStr[cloneStr.Length / 2] += "[" + firstLocation.node[2] + "," + firstLocation.node[0] + "," + newY + "]";
			//if (i == listIndex.Length) cloneStr[cloneStr.Length / 2] += "]";
			//else cloneStr[cloneStr.Length / 2] += ",";
			//Debug.Log(cloneStr[cloneStr.Length / 2]);
			//baseSymetric = firstLocation;

			if (mode == Mode.CloneFromBaseObject)
			{
				for (int j = 1; j < cloneStr.Length; j++)
				{
					Node cloneLocation = ConvertVeticle(listGPSLocation[index - 1].node[0], listGPSLocation[index - 1].node[2], cloneAngles[j - 1], cloneDistances[j - 1]);
					Node location = new Node(cloneLocation.node[0], listGPSLocation[index - 1].node[1], cloneLocation.node[2]);

					if (cloneStr[j] == null || cloneStr[j] == "") cloneStr[j] = "[";
					cloneStr[j] += "[" + location.node[2] + "," + location.node[0] + "," + newY + "]";
					if (i == listIndex.Length) cloneStr[j] += "]";
					else cloneStr[j] += ",";
				}

				//int temp = 1;
				//for (int k = cloneStr.Length / 2 + 1; k < cloneStr.Length; k++)
				//{
				//    double newAngle = newAngle = cloneAngles[temp - 1];
				//    if (cloneAngles[temp - 1] < 90)
				//    {
				//        newAngle = cloneAngles[temp - 1] + 90;
				//    }
				//    else if (cloneAngles[temp - 1] > 90)
				//    {
				//        newAngle = cloneAngles[temp - 1] - 90;
				//    }

				//    newAngle -= (180 - alphaFromNorth);
				//    Debug.Log((temp + 1).ToString() + " vs 1___" + " Distance: " + (cloneDistances[temp - 1]).ToString() + "m Angle: " + newAngle);

				//    Node cloneSymetricLocation = ConvertVeticle(baseSymetric.node[0], baseSymetric.node[2], newAngle, cloneDistances[temp - 1]);
				//    Node symetricLocation = new Node(cloneSymetricLocation.node[0], listGPSLocation[index - 1].node[1], cloneSymetricLocation.node[2]);

				//    if (cloneStr[k] == null || cloneStr[k] == "") cloneStr[k] = "[";
				//    cloneStr[k] += "[" + symetricLocation.node[2] + "," + symetricLocation.node[0] + "," + newY + "]";
				//    if (i == listIndex.Length) cloneStr[k] += "]";
				//    else cloneStr[k] += ",";
				//    temp++;
				//}
			}
		}

		//geometry.coordinates.Add(polygon);
		//string json = JsonUtility.ToJson(geometry);


		if (mode == Mode.CloneFromBaseObject)
		{
			string clone = "[";
			for (int h = 0; h < cloneStr.Length; h++)
			{
				clone += cloneStr[h];
				if (h == cloneStr.Length - 1) clone += "]";
				else clone += ",";
			}
			SaveSystem.Save(clone, "clone_" + fileName);
		}
		else SaveSystem.Save(str, fileName);
	}

	bool CompareNode(Vector3 a, Vector3 b)
	{
		return a.x - b.x == 0 && a.y - b.y == 0 && a.z - b.z == 0;
	}

	void ConvertAllVerticlesToGPSLocation()
	{
		int i = 0;
		foreach (Vector3 verticle in wVerticles)
		{
			if (!isFirstLocation)
			{
				//(0, 1) or Vector2.up is north in 2D
				Vector3 newVector3 = verticle - wVerticles[0];
				Vector3 newVector2 = new Vector2(newVector3.x, newVector3.z);
				float angle = Vector2.SignedAngle(newVector2, Vector2.up);
				if (angle < 0)
				{
					angle = 360 - angle * -1;
				}

				double distance = Math.Sqrt(Math.Pow(verticle.x - wVerticles[0].x, 2) + Math.Pow(verticle.z - wVerticles[0].z, 2)) * scale / 1000;

				//Debug.Log((i + 1).ToString() + "with 1: " + angle + ", " + (distance * 1000).ToString());

				Node verticleGPSLocation = ConvertVeticle(listGPSLocation[0].node[0], listGPSLocation[0].node[2], angle, distance);
				Node location = new Node(verticleGPSLocation.node[0], (verticle.y - yGround) * scale / 1000, verticleGPSLocation.node[2]);
				listGPSLocation[i] = location;
			}
			else
			{
				isFirstLocation = false;
			}
			//prevPoint = verticle;
			i++;
			if (i > listGPSLocation.Length) break;
		}

		//Debug.Log("Call Save after convert");
		//Save();
	}

	Node ConvertVeticle(double fmLat, double fmLon, double heading, double distanceKm)
	{
		double bearingR = ToRadians(heading);

		double latR = ToRadians(fmLat);
		double lonR = ToRadians(fmLon);

		double distanceToRadius = distanceKm / EarthRadius;


		double newLatR = Math.Asin(Math.Sin(latR) * Math.Cos(distanceToRadius) + Math.Cos(latR) * Math.Sin(distanceToRadius) * Math.Cos(bearingR));

		double newLonR = lonR + Math.Atan2(
																				Math.Sin(bearingR) * Math.Sin(distanceToRadius) * Math.Cos(latR),
																				Math.Cos(distanceToRadius) - Math.Sin(latR) * Math.Sin(newLatR)
																			 );
		return new Node(ToDegrees(newLatR), 0, ToDegrees(newLonR));
	}

	void SaveCloneProperties()
	{
		bool isFirstObject = true;
		foreach (Transform other in transform)
		{
			if (!isFirstObject)
			{
				//(0, 1) or Vector2.up is north in 2D
				Vector3 newVector3 = other.position - baseObject.transform.position;
				Vector3 newVector2 = new Vector2(newVector3.x, newVector3.z);
				float angle = Vector2.SignedAngle(newVector2, Vector2.up);
				if (angle < 0)
				{
					angle = 360 - angle * -1;
				}

				double distance = Math.Sqrt(Math.Pow(other.position.x - baseObject.transform.position.x, 2) + Math.Pow(other.position.z - baseObject.transform.position.z, 2)) * scale / 1000;
				cloneAngles.Add(angle);
				cloneDistances.Add(distance);

				//Debug.Log((cloneDistances.Count + 1).ToString() + " vs 1___" + " Distance: " + (distance).ToString() + "m Angle: " + angle);
			}
			else isFirstObject = false;
		}
	}

	//void SaveSymetricLocation()
	//{
	//    string str = "[";

	//    for (int i = 0; i < listLocation1.Length; i++)
	//    {

	//        Node symetricLocation = ConvertVeticle(listLocation1[i].node[0], listGPSLocation[i].node[2], alphaFromNorth, distanceToSymetricLocation);
	//        Node location = new Node(symetricLocation.node[0], listLocation1[i].node[1], symetricLocation.node[2]);

	//        str += "[" + location.node[2] + "," + location.node[0] + "," + location.node[1] + "]";
	//        if (i == str.Length) str += "]";
	//        else str += ",";
	//    }

	//    SaveSystem.Save(str, "symetric_" + fileName);
	//}

	public double ToRadians(double degrees)
	{
		return (Math.PI / 180) * degrees;
	}
	public double ToDegrees(double radians)
	{
		return (180 / Math.PI) * radians;
	}

	double GetDistanceFromToLocation(double lat1, double long1, double lat2, double long2)
	{
		// Convert the latitudes
		// and longitudes
		// from degree to radians.
		lat1 = ToRadians(lat1);
		long1 = ToRadians(long1);
		lat2 = ToRadians(lat2);
		long2 = ToRadians(long2);

		// Haversine Formula
		double dlong = long2 - long1;
		double dlat = lat2 - lat1;

		double a = Math.Pow(Math.Sin(dlat / 2), 2) +
							 Math.Cos(lat1) * Math.Cos(lat2) *
							 Math.Pow(Math.Sin(dlong / 2), 2);

		double c = 2 * Math.Asin(Math.Sqrt(a));

		// Radius of earth in
		// kilometers. Use 3956
		// for miles
		// calculate the result
		return (c * EarthRadius);
	}
}
