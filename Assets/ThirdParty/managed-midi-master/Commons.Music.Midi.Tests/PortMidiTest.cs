﻿// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Commons.Music.Midi.PortMidi;
// using NUnit.Framework;

// namespace Commons.Music.Midi.Tests
// {
// 	[TestFixture]
// 	public class PortMidiTest
// 	{
// 		[Test]
// 		public void DeviceDetails ()
// 		{
// 			var a = new PortMidiAccess ();
// 			var dic = new Dictionary<string,IMidiPortDetails> ();
// 			foreach (var i in a.Inputs)
// 				dic.Add (i.Id, i);
// 			foreach (var o in a.Outputs)
// 				dic.Add (o.Id, o);
				
// 			// mmk exposed some bug with this code in rtmidi.
// 			var devId = a.Outputs.First ().Id;
// 			var op = a.OpenOutputAsync (devId).Result;

// 			IMidiPortDetails dummy;
// 			foreach (var o in a.Outputs)
// 				if (!dic.TryGetValue (o.Id, out dummy))
// 					Assert.Fail ("Device ID " + o.Id + " was not found.");
// 			op.Dispose ();
// 		}
// 	}
// }

