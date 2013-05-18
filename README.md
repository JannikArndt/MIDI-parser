MIDI-parser
===========

A C# MIDI parser

Latest version compiled with mono: https://dm121506.students.fhstp.ac.at/Projects/MIDIParser/Midi.dll

Usage:

    FileStream file = System.IO.File.OpenRead ("your_file.mid");
    Midi.MidiData midi_data = Midi.FileParser.parse (file);
    
    // Header: midi_data.header
    // Tracks: midi_data.tracks
    // Track 1: midi_data.tracks[0]
    // Events of track 1: midi_data.tracks[0].events
    
    // More to come soon