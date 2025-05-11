using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CesiumForUnity;
using TMPro;

public class TeleportUI : MonoBehaviour
{
    [Header("Car reference")]
    public GameObject car; // car object to teleport
    public CesiumGeoreference georeference; // georeference component for converting coordinates

    [Header("UI elements")]
    public GameObject teleportWindow; // Teleport window container
    public Button openTeleportButton; // button to open the teleport window
    public Button closeTeleportButton; // button to close the teleport window
    public Button teleportButton; // button to execute the teleportation
    public Button saveButton; // button to save the provided coordinates
    public Button deleteButton; // button to save the selected coordinates
    public TMP_InputField latitudeInput; // user input for latitude
    public TMP_InputField longitudeInput; // user input for longitude
    public TMP_InputField heightInput; // user input for height
    public TMP_InputField locationNameInput; // user input for location name
    public TMP_Dropdown savedLocationsDropdown; // dropdown for saved locations

    [Header("Default location")] // default location is Reading Rail Station
    public double defaultLatitude = 51.4586;
    public double defaultLongitude = -0.9725;
    public double defaultHeight = 370;

    // Internal variables for storing saved locations
    private string savedLocationsFilePath;
    private Dictionary<string, Vector3d> savedLocations = new Dictionary<string, Vector3d>();

    // Data structure for saving locations
    [System.Serializable]
    private class LocationData
    {
        public string name;
        public double latitude;
        public double longitude;
        public double height;
    }

    // List of locations for JSON serialisation
    [System.Serializable]
    private class LocationsList
    {
        public List<LocationData> locations = new List<LocationData>();
    }

    private void Awake()
    {
        savedLocationsFilePath = Path.Combine(Application.persistentDataPath, "savedLocations.json");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Add listeners to all the buttons
        openTeleportButton.onClick.AddListener(OpenTeleportWindow);
        closeTeleportButton.onClick.AddListener(CloseTeleportWindow);
        saveButton.onClick.AddListener(SaveLocation);
        deleteButton.onClick.AddListener(DeleteSelectedLocation);
        teleportButton.onClick.AddListener(TeleportCar);
        
        // Set initial input field values to default location
        latitudeInput.text = defaultLatitude.ToString();
        longitudeInput.text = defaultLongitude.ToString();
        heightInput.text = defaultHeight.ToString();

        // Reset dropdown to avoid default values
        savedLocationsDropdown.ClearOptions();

        // Load saved locations
        LoadSavedLocations();
        
        // Add a placeholder option
        savedLocationsDropdown.options.Add(new TMP_Dropdown.OptionData("Saved Locations"));
        
        // Add saved locations if any exist
        UpdateDropdown();
        
        // Subscribe to dropdown change
        savedLocationsDropdown.onValueChanged.AddListener(OnSavedLocationSelected);

        // Start with teleport window closed
        CloseTeleportWindow();
    }

    public void SaveLocation()
    {
        string locationName = locationNameInput.text;
        
        // Handle cases where location name is empty
        if (string.IsNullOrEmpty(locationName))
        {
            return;
        }

        // Save location if provided values are valid
        if (double.TryParse(latitudeInput.text, out double latitude) &&
            double.TryParse(longitudeInput.text, out double longitude) &&
            double.TryParse(heightInput.text, out double height))
        {
            // Save the location
            Vector3d coordinates = new Vector3d(latitude, longitude, height);
            savedLocations[locationName] = coordinates;
            SaveLocationsToFile();
            
            // Update dropdown
            UpdateDropdown();
            
            // Clear location name field
            locationNameInput.text = "";
        }
    }

    private void OnSavedLocationSelected(int index)
    {
        // Skip the header item (index 0)
        if (index <= 0) return;

        string locationName = savedLocationsDropdown.options[index].text;
        if (savedLocations.TryGetValue(locationName, out Vector3d coordinates))
        {
            latitudeInput.text = coordinates.x.ToString();
            longitudeInput.text = coordinates.y.ToString();
            heightInput.text = coordinates.z.ToString();
        }
    }

    private void UpdateDropdown()
    {
        // Store the current selection to restore it in a fresh dropdown
        string currentSelection = null;
        if (savedLocationsDropdown.value > 0 && savedLocationsDropdown.value < savedLocationsDropdown.options.Count)
            currentSelection = savedLocationsDropdown.options[savedLocationsDropdown.value].text;
        
        // Clear all options
        savedLocationsDropdown.ClearOptions();
        
        // Start with re-adding the header
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Saved Locations")
        };
        
        // Add locations that have been saved
        foreach (string locationName in savedLocations.Keys)
        {
            options.Add(new TMP_Dropdown.OptionData(locationName));
        }

        savedLocationsDropdown.AddOptions(options);
        
        // Restore selection if possible
        if (!string.IsNullOrEmpty(currentSelection))
        {
            for (int i = 0; i < savedLocationsDropdown.options.Count; i++)
            {
                if (savedLocationsDropdown.options[i].text == currentSelection)
                {
                    savedLocationsDropdown.value = i;
                    break;
                }
            }
        }
    }

    private void SaveLocationsToFile()
    {
        // Create a new locations dictionary
        LocationsList locationsList = new LocationsList();
        
        // Loop over existing saved locations
        foreach (var location in savedLocations)
        {
            // Add each location to the list
            locationsList.locations.Add(new LocationData
            {
                name = location.Key,
                latitude = location.Value.x,
                longitude = location.Value.y,
                height = location.Value.z
            });
        }
        
        // Serialise the list to JSON and save it to file
        string json = JsonUtility.ToJson(locationsList, true);
        File.WriteAllText(savedLocationsFilePath, json);
    }

    private void LoadSavedLocations()
    {
        // Check if the save file exists
        if (File.Exists(savedLocationsFilePath))
        {
            try 
            {
                // Unpack (deserialise) the JSON file
                string json = File.ReadAllText(savedLocationsFilePath);
                LocationsList locationsList = JsonUtility.FromJson<LocationsList>(json);
                
                // Clear existing locations and repopulate with loaded data
                savedLocations.Clear();
                foreach (var location in locationsList.locations)
                {
                    savedLocations[location.name] = new Vector3d(location.latitude, location.longitude, location.height);
                }
            }
            catch (Exception e)
            {
                // Create a new file if the existing one is corrupted
                savedLocations.Clear();
                SaveLocationsToFile();
            }
        }
    }

    public void TeleportCar()
    {
        // Check if the input fields are valid
        if (double.TryParse(latitudeInput.text, out double latitude) &&
            double.TryParse(longitudeInput.text, out double longitude) &&
            double.TryParse(heightInput.text, out double height))
        {
            // Convert geographic coordinates to ECEF (Earth-Centered, Earth-Fixed)
            Unity.Mathematics.double3 lonLatHeight = new Unity.Mathematics.double3(longitude, latitude, height);
            
            // Use the correct API call for your Cesium version
            Unity.Mathematics.double3 position = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(lonLatHeight);
            
            // Convert ECEF to Unity world position
            Unity.Mathematics.double3 unityPosition = georeference.TransformEarthCenteredEarthFixedPositionToUnity(position);
            
            // Convert double3 to Vector3 for setting transform position
            car.transform.position = new Vector3((float)unityPosition.x, (float)unityPosition.y, (float)unityPosition.z);
            
            // Close the window after teleporting
            CloseTeleportWindow();
        }
    }

    public void OpenTeleportWindow()
    {
        teleportWindow.SetActive(true);
    }
    
    public void CloseTeleportWindow()
    {
        teleportWindow.SetActive(false);
    }
    
    // Helper method to delete a saved location
    public void DeleteSelectedLocation()
    {
        if (savedLocationsDropdown.value <= 0) return; // skip if header selected
        
        string locationName = savedLocationsDropdown.options[savedLocationsDropdown.value].text;
        if (savedLocations.ContainsKey(locationName))
        {
            savedLocations.Remove(locationName);
            UpdateDropdown();
            SaveLocationsToFile();
            
            // Reset dropdown to header
            savedLocationsDropdown.value = 0;
            
            // Clear inputs
            locationNameInput.text = "";
        }
    }
}

// Helper struct for 3D double precision vector
[System.Serializable]
public struct Vector3d
{
    public double x;
    public double y;
    public double z;

    public Vector3d(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}