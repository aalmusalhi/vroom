using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CesiumForUnity;
using TMPro;

public class TeleportUI : MonoBehaviour
{
    [Header("Car Reference")]
    public GameObject car;
    public CesiumGeoreference georeference;

    [Header("UI Elements")]
    public GameObject teleportWindow; // The modal window container
    public Button openTeleportButton; // Button to open the teleport window
    public Button closeTeleportButton; // Button to close the teleport window
    public Button teleportButton;
    public Button saveButton;
    public Button deleteButton;
    public TMP_InputField latitudeInput;
    public TMP_InputField longitudeInput;
    public TMP_InputField heightInput;
    public TMP_InputField locationNameInput;
    public TMP_Dropdown savedLocationsDropdown;

    [Header("Default Location")]
    public double defaultLatitude = 51.4586; // Reading Rail Station latitude
    public double defaultLongitude = -0.9725; // Reading Rail Station longitude
    public double defaultHeight = 370;

    private string savedLocationsFilePath;
    private Dictionary<string, Vector3d> savedLocations = new Dictionary<string, Vector3d>();

    [System.Serializable]
    private class LocationData
    {
        public string name;
        public double latitude;
        public double longitude;
        public double height;
    }

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
        // Add listeners to buttons
        openTeleportButton.onClick.AddListener(OpenTeleportWindow);
        closeTeleportButton.onClick.AddListener(CloseTeleportWindow);
        saveButton.onClick.AddListener(SaveLocation);
        deleteButton.onClick.AddListener(DeleteSelectedLocation);
        teleportButton.onClick.AddListener(TeleportCar);
        
        // Set initial input field values to default location
        latitudeInput.text = defaultLatitude.ToString();
        longitudeInput.text = defaultLongitude.ToString();
        heightInput.text = defaultHeight.ToString();

        // Load saved locations
        LoadSavedLocations();
        
        // Reset dropdown to avoid default values
        savedLocationsDropdown.ClearOptions();
        
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
        
        if (string.IsNullOrEmpty(locationName))
        {
            Debug.LogError("Please enter a location name!");
            return;
        }

        if (double.TryParse(latitudeInput.text, out double latitude) &&
            double.TryParse(longitudeInput.text, out double longitude) &&
            double.TryParse(heightInput.text, out double height))
        {
            // Save the location
            Vector3d coordinates = new Vector3d(latitude, longitude, height);
            savedLocations[locationName] = coordinates;
            
            // Update dropdown
            UpdateDropdown();
            
            // Save to file
            SaveLocationsToFile();
            
            // Clear location name field
            locationNameInput.text = "";
            
            Debug.Log($"Saved location '{locationName}': Lat {latitude}, Lng {longitude}, Height {height}");
        }
        else
        {
            Debug.LogError("Invalid coordinate format!");
        }
    }

    private void OnSavedLocationSelected(int index)
    {
        // Skip if it's the header item (index 0)
        if (index <= 0) return;

        string locationName = savedLocationsDropdown.options[index].text;
        if (savedLocations.TryGetValue(locationName, out Vector3d coordinates))
        {
            latitudeInput.text = coordinates.x.ToString();
            longitudeInput.text = coordinates.y.ToString();
            heightInput.text = coordinates.z.ToString();
            
            Debug.Log($"Selected location: {locationName} with coordinates: Lat {coordinates.x}, Lng {coordinates.y}, Height {coordinates.z}");
        }
    }

    private void UpdateDropdown()
    {
        // Store the current selection
        string currentSelection = null;
        if (savedLocationsDropdown.value > 0 && savedLocationsDropdown.value < savedLocationsDropdown.options.Count)
        {
            currentSelection = savedLocationsDropdown.options[savedLocationsDropdown.value].text;
        }
        
        // Clear all options
        savedLocationsDropdown.ClearOptions();
        
        // Add header
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Saved Locations")
        };
        
        // Add locations
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
        LocationsList locationsList = new LocationsList();
        
        foreach (var location in savedLocations)
        {
            locationsList.locations.Add(new LocationData
            {
                name = location.Key,
                latitude = location.Value.x,
                longitude = location.Value.y,
                height = location.Value.z
            });
        }
        
        string json = JsonUtility.ToJson(locationsList, true);
        File.WriteAllText(savedLocationsFilePath, json);
        
        Debug.Log($"Saved locations to: {savedLocationsFilePath}");
    }

    private void LoadSavedLocations()
    {
        if (File.Exists(savedLocationsFilePath))
        {
            try 
            {
                string json = File.ReadAllText(savedLocationsFilePath);
                LocationsList locationsList = JsonUtility.FromJson<LocationsList>(json);
                
                savedLocations.Clear();
                foreach (var location in locationsList.locations)
                {
                    savedLocations[location.name] = new Vector3d(location.latitude, location.longitude, location.height);
                }
                
                Debug.Log($"Loaded {savedLocations.Count} saved locations");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading saved locations: {e.Message}");
                // Create a new file if the existing one is corrupted
                savedLocations.Clear();
                SaveLocationsToFile();
            }
        }
    }

    public void TeleportCar()
    {
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
            
            Debug.Log($"Teleported car to: Lat {latitude}, Lng {longitude}, Height {height}");
            
            // Close the window after teleporting
            CloseTeleportWindow();
        }
        else
        {
            Debug.LogError("Invalid coordinate format!");
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
        if (savedLocationsDropdown.value <= 0) return; // Skip if header selected
        
        string locationName = savedLocationsDropdown.options[savedLocationsDropdown.value].text;
        if (savedLocations.ContainsKey(locationName))
        {
            savedLocations.Remove(locationName);
            UpdateDropdown();
            SaveLocationsToFile();
            Debug.Log($"Deleted location: {locationName}");
            
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