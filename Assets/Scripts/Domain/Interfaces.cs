using System.Collections.Generic;
using UnityEngine;

namespace RoboterLego.Domain
{
    public interface ISessionController
    {
        SessionState CurrentState { get; }
        void StartSession();
        void ResetSession();
    }

    public interface IGenerator
    {
        GenerationSeed CreateSeed(InputFeatures features);
        RobotBlueprint Generate(InputFeatures features, GenerationSeed seed);
    }

    public interface IBehaviorController
    {
        void BindRobot(GameObject robotInstance, RobotBlueprint blueprint);
        void TickMovement(Vector2 directionalInput, float deltaTime);
        void PlayDance();
        void PlaySing();
        void StopAllActions();
    }

    public interface IAudioCueService
    {
        void PlayCreateCue();
        void PlayGenerateCue();
        void PlayPlayCue();
        void PlayBuildSequence(float intensity);
        void SetMovementLoop(bool isMoving, float speedNormalized);
        void PlayDanceMusic(string danceStyle, float energy);
        void StopDanceMusic();
    }

    public interface ILegoAssetProvider
    {
        GameObject LoadPrefab(string prefabRef, ModuleSpec moduleSpec = null);
    }

    public interface ILegoAssetProviderChain : ILegoAssetProvider
    {
        IReadOnlyList<ILegoAssetProvider> Providers { get; }
    }

    public interface IShapeRecognizer
    {
        ShapeType Recognize(IReadOnlyList<Vector2> points);
    }
}
