// *******************************************************
// Copyright 2013 Daikon Forge, all rights reserved under 
// US Copyright Law and international treaties
// *******************************************************
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class SaveOnPlay
{
	
	static SaveOnPlay()
	{
		
		EditorApplication.playmodeStateChanged = () =>
		{
			
			if( EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying )
			{
				
				Debug.Log( "Auto-Saving scene before entering Play mode: " + EditorApplication.currentScene );
				
				EditorApplication.SaveScene();
				AssetDatabase.SaveAssets();
			}
			
		};
		
	}
	
}