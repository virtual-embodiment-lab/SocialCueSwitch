# SocialCueSwitch Package for Unity

![Hero Image](Images/teaser.png)

## Overview of the Unity Package

The SocialCueSwitch Unity package is designed to enhance virtual interactions within Unity-powered environments, focusing on improving communication and engagement between avatars through a sophisticated system of audio and visual cues based on proximity and gestures. This package includes a range of pre-configured assets, scripts, and demo scenes to facilitate rapid integration and customization tailored to specific project requirements.

## Installation

1. **Import Package**: Start by importing the `SocialCueSwitch` package into your Unity project.
2. **Check Dependencies**: Ensure your project includes all necessary dependencies such as `UnityEngine`, `UnityEngine.UI`, and `TMPro`.

## Setting Up

1. **Demo Scene**: To get familiar with the system, navigate to the demo scene included with the package.
2. **Script Attachment**: Attach the `SocialCueSwitch` script to your character controller or any avatar that should display social cues.

![audiotovisual](Images/audiotovisual.png)

## Configuration

Customize the `SocialCueSwitch` script to fit your project needs:

- **Tone Configurations**: Set audio clips and volume thresholds for different proximity observations and gesture tones.
- **Avatar Configurations**: Define and manage observing objects, nearby avatars, and their respective audio settings.
- **Arrow Configurations**: Adjust the visual indicator arrow, including its appearance and position, to point towards the active talking object.
- **Caption Settings**: Customize the captions' appearance, including font and timing, to effectively communicate social cues.

## Gestures

1. **Gesture Recognition**: Ensure your avatars are equipped with the necessary components for gesture detection.
2. **Audio Feedback**: Use the `PlayGestureAudio` method to play an audio cue when a gesture is recognized.

## Audio Cues

1. **Assign Audio Clips**: Choose appropriate clips for proximity alerts and gesture-related tones.
2. **Volume Control**: Adjust volume thresholds to manage when each tone should be played or escalated in volume.

## Visual Cues

1. **Arrow Indicator**: Configure the arrow prefab to point towards the current speaking avatar, adjusting its position and rotation as needed.
2. **Captions**: Set up captions to display based on social cues, customizing their appearance and display duration.

![visualtoaudio](Images/visualtoaudio.png)

## Testing

1. **Interaction Testing**: Use the demo scene to interact with other avatars and test the system's feedback.
2. **Observe Cues**: Pay attention to the audio and visual cues triggered by proximity, eye contact, or gestures.
3. **Configuration Adjustments**: Modify the settings as necessary to achieve the desired behavior for your project.

## Advanced Usage

1. **Extend Functionality**: Advanced users can expand the script's capabilities by adding custom methods for specific events or interactions.
2. **Custom Feedback**: Utilize provided methods, such as `AddCaption`, to offer tailored feedback based on unique scenarios.

## Version History

Version 1.0: Initial release of the SocialCueSwitch Unity package.
