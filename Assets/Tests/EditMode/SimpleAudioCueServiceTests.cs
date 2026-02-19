using NUnit.Framework;
using RoboterLego.Behavior;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class SimpleAudioCueServiceTests
    {
        private GameObject host;
        private SimpleAudioCueService service;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("AudioCueServiceTests");
            host.AddComponent<AudioSource>();
            service = host.AddComponent<SimpleAudioCueService>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void ProceduralClipBuilders_ReturnValidClips()
        {
            AudioClip buildClip = service.CreateBuildSequenceClip(0.6f);
            AudioClip moveClip = service.CreateMovementLoopClip(0.5f);
            AudioClip danceClip = service.CreateDanceLoopClip("spin_bounce", 0.7f);

            Assert.NotNull(buildClip);
            Assert.NotNull(moveClip);
            Assert.NotNull(danceClip);
            Assert.Greater(buildClip.samples, 1000);
            Assert.Greater(moveClip.samples, 1000);
            Assert.Greater(danceClip.samples, 1000);
        }
    }
}
