﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity;
using System.IO;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using Random = UnityEngine.Random;

namespace Simulation {
    public enum Type {
        Coupe,
        Sedan,
        SUV,
        Hatchback,
        Convertible
    }

    public enum Accessory {
        Tissue,
        Coffee,
        BabySeat,
        Bag
    }

    public enum SeatRole {
        Driver,
        Passenger
    }

    public enum Color {
        Red
    }

    public enum Texture {
        Leather,
        Fabric
    }

    [Serializable]
    public class Seat {
        public Vector3 position;
    }

    [Serializable]
    public class AbsolutePosition {
        public Seat[] seats;
        public CameraDistribution cameraRange;

        public void Load(string aJson) {
            JsonUtility.FromJsonOverwrite(aJson, this);
        }
    }


    [Serializable]
    public class LightDistribution {
        public Utils.Distribution intensityRange;
        private Light _light;

        public void BuildLight(GameObject parent) {
            var intensity = Utils.SelectProperty(intensityRange.range);
            _light = new Light(intensity, parent);
        }
    }

    public class Light {
        private float _intensity;
        private GameObject _instance;

        public Light(float intensity, GameObject parent) {
            _intensity = intensity;
            Instantiate(parent);
        }

        public void Instantiate(GameObject parent) {
            _instance = new GameObject("Generated Light");
            _instance.AddHDLight(HDLightTypeAndShape.Directional);
            _instance.GetComponent<HDAdditionalLightData>().intensity = _intensity;
            _instance.GetComponent<HDAdditionalLightData>().EnableColorTemperature(true);
            _instance.GetComponent<HDAdditionalLightData>().SetColor(UnityEngine.Color.white, 7000F);

            _instance.transform.parent = parent.transform;
            _instance.transform.position = new Vector3(0, 0, 0);
        }
    }

    [Serializable]
    public class CarType {
        public Type type;
        public Color color;
        public string prefabPath;

        public void Load(string aJson) {
        }
    }

    [Serializable]
    public class AnimationType {
        public SeatRole seatRole;
        public string prefabPath;

        public void Load(string aJson) {
            JsonUtility.FromJsonOverwrite(aJson, this);
        }
    }

    [Serializable]
    public class CarDistribution {
        public PassengerDistribution passengerRange;
        public Utils.Distribution numPassengersRange;
        public Utils.Distribution typeRange;
        public Utils.Distribution colorRange;
        public LightDistribution lightRange;

        public CarType[] cars;
        private const string CarPath = "Prefabs/Car/";
        private Car _car;
        private Camera _camera;

        public CarDistribution() {
            LoadCars();
        }

        public void LoadCars() {
            var targetFile = Resources.Load<TextAsset>("Config/car_assets");
            JsonUtility.FromJsonOverwrite(targetFile.text, this);
        }

        public void SelectAndBuildCar() {
            var filterPath = new string[Enum.GetValues(typeof(Utils.CarIndex)).Length];
            var type = (Type) Utils.SelectProperty(typeRange.range);
            var color = (Color) Utils.SelectProperty(colorRange.range);
            var numPassengers = Utils.SelectProperty(numPassengersRange.range);
            Debug.Log("Type: " + type + ", Color: " + color + ", Numpass: " + numPassengers);

            //Select the car prefab
            CarType[] viableCars = cars.Where(car => {
                var viable = car.color == color && car.type == type;
                return viable;
            }).ToArray();
            Utils.RangeInt carRange = new Utils.RangeInt(0, viableCars.Length);
            int selectedCarIndex = Utils.SelectProperty(carRange);
            CarType selectedCar = viableCars[selectedCarIndex];
            Debug.Log("Model of Car: " + selectedCar.prefabPath);

            //Type Filter Path
            Utils.FillPath(filterPath, Utils.CarIndex.Type, Enum.GetName(typeof(Type), type),
                "Car Type Specification Error!");
            //Color Filter Path
            Utils.FillPath(filterPath, Utils.CarIndex.Color, Enum.GetName(typeof(Color), color),
                "Car Type Specification Error!");

            _car = new Car(type, color, numPassengers, selectedCar.prefabPath,
                Path.Combine(CarPath, Path.Combine(filterPath), selectedCar.prefabPath));
        }

        public void BuildLight() {
            lightRange.BuildLight(_car.Instance);
        }

        public void BuildCamera() {
            _car.BuildCamera();
        }

        public void FillPassengers() {
            var driver = passengerRange.SelectAndBuildPassenger(_car.Instance);
            _car.AssignSeating(driver, true);
            foreach (var i in Enumerable.Range(1, _car.NumPassengers - 1)) {
                var passenger = passengerRange.SelectAndBuildPassenger(_car.Instance);
                _car.AssignSeating(passenger);
            }
        }
    }

    public class Car {
        public int NumPassengers { get; }
        public GameObject Instance { get; private set; }
        public GameObject Env { get; private set; }

        public List<Tuple<Passenger, Seat>> Passengers => _passengers;

        public AnimationType[] animations;

        //public Camera camera;
        // public Accessory[] carAccessories; //objects in car ie:tissue box
        private Type _type;
        private Color _color;
        private string _selectedCar;
        private AbsolutePosition _positions;
        private List<Tuple<Passenger, Seat>> _passengers = new List<Tuple<Passenger, Seat>>();

        public Car(Type type, Color color, int numPassengers, string selectedCar, string assetPath) {
            _type = type;
            _color = color;
            NumPassengers = numPassengers;
            _selectedCar = selectedCar;
            LoadAbsolutePositions();
            LoadAnimation();
            Instantiate(assetPath);
        }


        public void LoadAnimation() {
            var targetFile = Resources.Load<TextAsset>("Config/animation_assets");
            JsonUtility.FromJsonOverwrite(targetFile.text, this);
        }

        private void LoadAbsolutePositions() {
            var targetFile = Resources.Load<TextAsset>("Config/" + _selectedCar);
            _positions = new AbsolutePosition();
            _positions.Load(targetFile.text);
        }

        public void AssignSeating(Passenger passenger, bool driver = false) {
            var seatPosition = driver ? 0 : _passengers.Count;
            passenger.AssignSeat(_positions.seats[seatPosition]);
            var filterPath = new string[2];
            //Select the animation prefab
            AnimationType[] viableAnimations = animations.Where(animation => {
                var viable = driver ? animation.seatRole == SeatRole.Driver : animation.seatRole == SeatRole.Passenger;
                return viable;
            }).ToArray();
            Utils.RangeInt animationRange = new Utils.RangeInt(0, viableAnimations.Length);
            int selectedAnimationIndex = Utils.SelectProperty(animationRange);
            Debug.Log(selectedAnimationIndex + " ||| " + viableAnimations.Length);
            AnimationType selectedAnimation = viableAnimations[selectedAnimationIndex];
            Debug.Log("Animation of passenger: " + selectedAnimation.prefabPath);
            filterPath[0] = "Animators";
            filterPath[1] = driver ? "Driver" : "Passenger";
            passenger.ApplyAnimation(Path.Combine("Prefabs/Animations/", Path.Combine(filterPath),
                selectedAnimation.prefabPath));
            var seating = new Tuple<Passenger, Seat>(passenger, _positions.seats[seatPosition]);
            _passengers.Add(seating);
        }

        private void Instantiate(string assetPath) {
            Env = UnityEngine.Object.Instantiate(Resources.Load("HDRPDefaultResources/DefaultSceneRoot")) as GameObject;
            Instance =
                UnityEngine.Object.Instantiate(Resources.Load(assetPath)) as GameObject;
            if (Instance == null) throw new Exception("Unable to load car asset");
            Instance.name = _selectedCar;
            Instance.transform.position = new Vector3(0, 0, 0);
        }

        public void BuildCamera() {
            _positions.cameraRange.BuildCamera(Instance);
        }
    }
}