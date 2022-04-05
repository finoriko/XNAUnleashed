using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using XELibrary;
using Microsoft.Xna.Framework.Net;

namespace ChaseAndEvade
{
    public class Player
    {
        private InputHandler input;
        private PlayingState playingState;

        private Vector3 playerInput;

        public int Score = 0;
        public byte Id;

        private float playerMoveUnit = 25;

        public PlayerIndex PlayerIndex;

        public float CurrentZ = 0;

        private PlayerState simulationState;

        Random rand = new Random();

        //Prediction / Smoothing
        private PlayerState previousState;
        private PlayerState displayState;

        // Used to interpolate displayState from previousState toward simulationState.
        private float currentSmoothing;

        private TimeSpan oneFrame = TimeSpan.FromSeconds(1.0 / 60.0);

        private RollingAverage clockDelta = new RollingAverage(100);

        public Player(Game game)
        {
            input = (InputHandler)game.Services.GetService(
               typeof(IInputHandler));

            playingState = (PlayingState)game.Services.GetService(
                typeof(IPlayingState));

            ResetPosition();

            //Prediction / Smoothing
            // Initialize all three versions of our state to the same values.
            previousState = displayState = simulationState;
        }

        public Color Color;

        private struct PlayerState
        {
            public Vector3 Position;
            public Vector3 Velocity;
        }

        public Vector3 HandleInput(PlayerIndex playerIndex)
        {
            PlayerIndex = playerIndex;

            playerInput = new Vector3(input.GamePads[
                                         (int)playerIndex].ThumbSticks.Left, 0);

            //restrict to 2D?
            if (!playingState.RestrictToXY)
            {
                if (input.GamePads[(int)playerIndex].Triggers.Right > 0)
                    playerInput.Z--;
                else if (input.GamePads[(int)playerIndex].Triggers.Left > 0)
                    playerInput.Z++;
            }

            if (input.GamePads[(int)playerIndex].Triggers.Right > 0)
                CurrentZ--;
            else if (input.GamePads[(int)playerIndex].Triggers.Left > 0)
                CurrentZ++;

            //Normalize our vector so we don’t go faster
            //when heading in multiple directions
            if (playerInput.LengthSquared() != 0)
                playerInput.Normalize();

            return (playerInput);
        }

        public void ResetPosition()
        {
            simulationState.Position = new Vector3(-rand.Next(300), 0, -300);
        }

        public void SetPosition(Vector3 position)
        {
            simulationState.Position = position;
        }

        public Vector3 Position
        {
            get { return (displayState.Position); }
        }

        public Vector3 Velocity
        {
            get { return (displayState.Velocity); }
        }

        /// <summary>
        /// Moves a locally controlled player in response to the specified inputs.
        /// </summary>
        public void UpdateLocal(Vector3 playerInput)
        {
            this.playerInput = playerInput;

            // Update the master simulation state.
            UpdateState(ref simulationState);

            //Prediction / Smoothing
            // Locally controlled players have no prediction or smoothing, so we
            // just copy the simulation state directly into the display state.
            displayState = simulationState;
        }

        /// <summary>
        /// Updates one of our state structures, using the current inputs to turn
        /// the player, and applying the velocity and inertia calculations. This
        /// method is used directly to update locally controlled players, and also
        /// indirectly to predict the motion of remote players.
        /// </summary>
        private void UpdateState(ref PlayerState state)
        {
            state.Velocity = playerInput;

            state.Velocity *= playerMoveUnit;

            // Update the position and velocity.
            state.Position += state.Velocity;

            playingState.KeepWithinBounds(ref state.Position, ref state.Velocity);
        }

        /// <summary>
        /// Writes our local player state into a network packet.
        /// </summary>
        public void WriteNetworkPacket(PacketWriter packetWriter, GameTime gameTime)
        {
            // Send our current time.
            packetWriter.Write((float)gameTime.TotalGameTime.TotalSeconds);

            // Send the current state of the player.
            packetWriter.Write(simulationState.Position);
            packetWriter.Write(simulationState.Velocity);
            packetWriter.Write(Score);

            // Also send our current inputs. These can be used to more accurately
            // predict how the player is likely to move in the future.
            packetWriter.Write(playerInput);

        }

        /// <summary>
        /// Reads the state of a remotely controlled player from a network packet.
        /// </summary>
        public void ReadNetworkPacket(PacketReader packetReader,
                                    GameTime gameTime, TimeSpan latency)
        {
            // Start a new smoothing interpolation from our current
            // state toward this new state we just received.
            previousState = displayState;
            currentSmoothing = 1;

            // Read what time this packet was sent.
            float packetSendTime = packetReader.ReadSingle();

            // Read simulation state from the network packet.
            simulationState.Position = packetReader.ReadVector3();
            simulationState.Velocity = packetReader.ReadVector3();
            Score = packetReader.ReadInt32();

            // Read remote inputs from the network packet.
            playerInput = packetReader.ReadVector3();

            // apply prediction to compensate for
            // how long it took this packet to reach us.
            ApplyPrediction(gameTime, latency, packetSendTime);
        }

        /// <summary>
        /// Incoming network packets tell us where the player was at the time the packet
        /// was sent. But packets do not arrive instantly! We want to know where the
        /// player is now, not just where it used to be. This method attempts to guess
        /// the current state by figuring out how long the packet took to arrive, then
        /// running the appropriate number of local updates to catch up to that time.
        /// This allows us to figure out things like "it used to be over there, and it
        /// was moving that way while turning to the left, so assuming it carried on
        /// using those same inputs, it should now be over here".
        /// </summary>
        private void ApplyPrediction(GameTime gameTime, TimeSpan latency, float packetSendTime)
        {
            // Work out the difference between our current local time
            // and the remote time at which this packet was sent.
            float localTime = (float)gameTime.TotalGameTime.TotalSeconds;

            float timeDelta = localTime - packetSendTime;

            // Maintain a rolling average of time deltas from the last 100 packets.
            clockDelta.AddValue(timeDelta);

            // The caller passed in an estimate of the average network latency, which
            // is provided by the XNA Framework networking layer. But not all packets
            // will take exactly that average amount of time to arrive! To handle
            // varying latencies per packet, we include the send time as part of our
            // packet data. By comparing this with a rolling average of the last 100
            // send times, we can detect packets that are later or earlier than usual,
            // even without having synchronized clocks between the two machines. We
            // then adjust our average latency estimate by this per-packet deviation.

            float timeDeviation = timeDelta - clockDelta.AverageValue;

            latency += TimeSpan.FromSeconds(timeDeviation);

            // Apply prediction by updating our simulation state however
            // many times is necessary to catch up to the current time.
            while (latency >= oneFrame)
            {
                UpdateState(ref simulationState);

                latency -= oneFrame;
            }
        }

        /// <summary>
        /// Applies prediction and smoothing to a remotely controlled player.
        /// </summary>
        public void UpdateRemote(int framesBetweenPackets)
        {
            // Update the smoothing amount, which interpolates from the previous
            // state toward the current simultation state. The speed of this decay
            // depends on the number of frames between packets: we want to finish
            // our smoothing interpolation at the same time the next packet is due.
            float smoothingDecay = 1.0f / framesBetweenPackets;

            currentSmoothing -= smoothingDecay;

            if (currentSmoothing < 0)
                currentSmoothing = 0;

            // Predict how the remote player will move by updating
            // our local copy of its simultation state.
            UpdateState(ref simulationState);

            //if we still need to smooth, update the previous state 
            //and actually apply the smoothing
            if (currentSmoothing > 0)
            {
                UpdateState(ref previousState);

                // Interpolate the display state gradually from the
                // previous state to the current simultation state.
                ApplySmoothing();
            }
            else
            {
                // Copy the simulation state directly into the display state.
                displayState = simulationState;
            }
        }


        /// <summary>
        /// Applies smoothing by interpolating the display state somewhere
        /// in between the previous state and current simulation state.
        /// </summary>
        private void ApplySmoothing()
        {
            displayState.Position = Vector3.Lerp(simulationState.Position,
                                                 previousState.Position,
                                                 currentSmoothing);

            displayState.Velocity = Vector3.Lerp(simulationState.Velocity,
                                                 previousState.Velocity,
                                                 currentSmoothing);
        }
    }
}
