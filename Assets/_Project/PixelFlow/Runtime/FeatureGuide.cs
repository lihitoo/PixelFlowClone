using System.Collections.Generic;

namespace PixelFlow
{
    public static class FeatureGuide
    {
        private static readonly Dictionary<string, string> Tips = new()
        {
            ["SurpriseShooters"] = "A hidden pig reveals color and ammo at the queue front or when it launches.",
            ["ConnectedShooters"] = "Connected pigs launch together. Make conveyor room for the entire group.",
            ["ChainedShooters"] = "Chained pigs launch together, including returning pigs. Finished pigs leave the group.",
            ["Locks"] = "Expose board keys to remove queue locks.",
            ["Key"] = "Clear a line of sight from a board edge to collect this key and open one queue lock.",
            ["ShooterPipes"] = "PIPE pigs emerge in their payload order. Each has its own ammo.",
            ["Hammers"] = "Tap a front hammer to strike wood. It uses no regular ammo or conveyor space.",
            ["Wood"] = "Wood protects pixels. Front hammers strike its layers.",
            ["ShooterIceBlocks"] = "Frozen pigs cannot launch. Successful board hits reduce their ICE counter.",
            ["BullTotems"] = "Totems block a queue. Their counter falls when normal pigs finish their ammo.",
            ["MusicToyMallets"] = "A front mallet strikes the matching current music-toy segment, then leaves.",
            ["MusicToy"] = "Music toys protect pixels. Use matching front mallets to remove colored segments.",
            ["SlotCages"] = "Sealed slots cannot accept returning pigs. Matching-color hits remove their cage health.",
            ["AstronautShooters"] = "Launching a space pig opens a matching spaceship shield. It keeps its ammo.",
            ["Spaceship"] = "Spaceships protect pixels until a matching astronaut launches.",
            ["ConveyorDoors"] = "Matching pigs damage doors. Other colors return early while the door is closed.",
            ["HardPixels"] = "Large pixels can need multiple hits. Their whole footprint shares one health counter.",
            ["SurprisePixels"] = "Hidden pixels reveal their color after their first matching hit.",
            ["Gate"] = "Shoot matching colors to remove the gate's health and footprint.",
            ["PixelPipe"] = "Board pipes contain a queue of colored layers. Clear them in order.",
            ["Snake"] = "Snakes shrink with matching hits, exposing more of the board.",
            ["MultiSnake"] = "Multi-color snakes change color between segments and shrink as they lose health.",
            ["Wall"] = "Walls block shots. Use exposed targets on another side of the conveyor.",
            ["EggBox"] = "Egg boxes hold colored health layers. Clear them with matching ammo.",
            ["Ice"] = "Ice shields its footprint. Successful hits elsewhere chip away the ice counter.",
            ["ColorDoor"] = "Colored shields lose health when successful board hits match their current color.",
            ["Biscuit"] = "Biscuit layers crumble through successful board hits.",
            ["Split"] = "Split obstacles shrink through successful hits, opening new lines of sight.",
            ["Ufo"] = "UFO seats fill with matching colors when pigs use all their ammo. Fill the seats to open the shield.",
            ["Pumpkin"] = "Pumpkins contain colored health layers. Clear each layer to reveal the next.",
            ["Curtain"] = "Matching board hits remove the curtain's current colored segment.",
            ["ShooterCage"] = "Expose this cage from a board edge to release its pig into a queue.",
            ["ColoredCage"] = "Shoot matching colors to clear the cage, then its captive pig joins a queue.",
            ["BeanBox"] = "Bean boxes contain colored health groups. Clear them in order.",
            ["Crossbow"] = "Crossbows are colored targets with their own health and occupied cells.",
            ["Accordion"] = "Matching board hits shrink the accordion and expose protected pixels.",
            ["Matryoshka"] = "Nesting dolls reveal their next colored layer when the current one breaks.",
            ["Mixer"] = "Matching hits shrink a mixer, opening the cells it protects."
        };
        public static string Text(string feature) => Tips.TryGetValue(feature, out var text) ? text : "Clear the conditions shown on the board.";
    }
}
