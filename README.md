# FluidSPH
Based on the work of Sebastian Lague, this fluid simulation uses Smoothed-Particle Hydrodynamics to represent accurate fluid motion.
Several updates have been made to Lague's work, including turning water particles into a struct so that it can be a single StructuredBuffer, the ability to collide with gameObjects, and changes to the Compute Shader's equations for performance gains.
