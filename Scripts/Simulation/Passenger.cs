﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Simulation {
    public enum Gender {
        Male,
        Female
    }

    public enum FacialFeatures {
        Mustache,
        Beard,
        Glasses
    }

    public enum Age {
        Adult,
        Elderly,
        Baby,
        Child
    }

    public enum Distraction {
        Alert,
        Distracted,
        Sleepy
    }

    [Serializable]
    public class PassengerType {
        public Gender gender;
        public Age age;
        public string prefabPath;
    }

    [Serializable]
    public class PassengerDistribution {
        public Utils.Distribution genderRange;
        public Utils.Distribution ageRange;

        public PassengerType[] passengers;

        private static List<string> _selectedPassengers = new List<string>();

        private const string PassengerPath = "Prefabs/Passenger/";

        private string[] _filterPath = new string [Enum.GetValues(typeof(Utils.PassengerIndex)).Length];

        public PassengerDistribution() {
            LoadPassenger();
        }

        public void LoadPassenger() {
            var targetFile = Resources.Load<TextAsset>("Config/people_assets");
            JsonUtility.FromJsonOverwrite(targetFile.text, this);
        }

        public static void Clear() {
            _selectedPassengers.Clear();
        }

        public Passenger SelectAndBuildPassenger(GameObject parent) {
            var gender = (Gender) Utils.SelectProperty(genderRange.range);
            Debug.Log("GENDER: " + gender);
            var age = (Age) Utils.SelectProperty(ageRange.range);
            Debug.Log("Age: " + age);
            //Select the passenger prefab
            PassengerType[] viablePassengers = passengers.Where(passenger => {
                var viable = passenger.gender == gender && passenger.age == age;
                foreach (var passengerName in _selectedPassengers) {
                    if (!viable)
                        break;
                    viable = passengerName != passenger.prefabPath;
                }

                return viable;
            }).ToArray();
            Utils.RangeInt passengerRange = new Utils.RangeInt(0, viablePassengers.Length);
            int selectedPassengerIndex = Utils.SelectProperty(passengerRange);
            Debug.Log(selectedPassengerIndex + " ||| " + viablePassengers.Length);
            PassengerType selectedPassenger = viablePassengers[selectedPassengerIndex];
            _selectedPassengers.Add(selectedPassenger.prefabPath);
            Debug.Log("Passenger of Car: " + selectedPassenger.prefabPath);

            //Gender Filter
            Utils.FillPath(_filterPath, Utils.PassengerIndex.GENDER, Enum.GetName(typeof(Gender), gender),
                "Gender Specification Error!");
            //Age Filter
            Utils.FillPath(_filterPath, Utils.PassengerIndex.AGE, Enum.GetName(typeof(Age), age),
                "Age Specification Error!");
            return new Passenger(gender, age, selectedPassenger.prefabPath, parent,
                Path.Combine(PassengerPath, Path.Combine(_filterPath), selectedPassenger.prefabPath));
        }
    }

    public class Passenger {
        private static List<Seat> _takenSeats = new List<Seat>();

        public RGB skinColor; //  white , brown , black
        public FacialFeatures[] facialFeatures; // "mustache" , "beard" , glasses
        public Clothes clothingColors; // RGB 
        public Distraction distractionLevel; // alert , distracted , sleepy
        public int weight; // 0 --> infinity
        public int height; // 0 --> infinity


        private Gender _gender; // (male :0, female :1)
        private Age _age; // 0 --> infinity
        private string _selectedPassenger;
        private GameObject _instance;

        public Passenger(Gender gender, Age age, string selectedPassenger, GameObject parent, string assetPath) {
            _gender = gender;
            _age = age;
            _selectedPassenger = selectedPassenger;
            Instantiate(parent, assetPath);
        }

        public void AssignSeat(Seat seat) {
            _instance.transform.localPosition = seat.position;
            _instance.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
        }

        public void ApplyAnimation(string controllerPath) {
            var animator = _instance.gameObject.GetComponent<Animator>();
            Debug.Log(controllerPath);
            animator.runtimeAnimatorController = Resources.Load(controllerPath) as RuntimeAnimatorController;
        }

        private void Instantiate(GameObject parent, string assetPath) {
            Debug.Log(assetPath);
            _instance = UnityEngine.Object.Instantiate(Resources.Load(assetPath),
                parent.transform, true) as GameObject;
            if (_instance == null) throw new Exception("Unable to load car asset");
            _instance.name = _selectedPassenger;
        }
    }

    [Serializable]
    public class Clothes {
        public RGB tshirt;
        public RGB pants;
    }
}