﻿/*
 * ITextureShader.cs - Interface for all textured shaders
 *
 * Copyright (C) 2024  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Amber.
 *
 * Amber is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Amber is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Amber. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Amber.Renderer.OpenGL.Shaders;

internal interface ITextureShader : IShader
{
	void SetTexture(int textureUnit = 0);

	void SetAtlasSize(uint width, uint height);

	void UsePalette(bool use);
}
