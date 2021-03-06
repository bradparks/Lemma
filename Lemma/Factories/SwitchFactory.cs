﻿using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SwitchFactory : Factory<Main>
	{
		public SwitchFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Switch");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;

			PointLight light = entity.GetOrCreate<PointLight>("PointLight");
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);
			attachable.Enabled.Value = true;

			light.Add(new Binding<Vector3>(light.Position, () => Vector3.Transform(new Vector3(0, 0, attachable.Offset), transform.Matrix), attachable.Offset, transform.Matrix));

			Switch sw = entity.GetOrCreate<Switch>("Switch");

			if (main.EditorEnabled)
				light.Enabled.Value = true;
			else
			{
				light.Add(new Binding<bool>(light.Enabled, sw.On));
				CommandBinding<IEnumerable<Voxel.Coord>, Voxel> cellFilledBinding = null;

				entity.Add(new NotifyBinding(delegate()
				{
					Voxel m = attachable.AttachedVoxel.Value.Target.Get<Voxel>();
					if (cellFilledBinding != null)
						entity.Remove(cellFilledBinding);

					cellFilledBinding = new CommandBinding<IEnumerable<Voxel.Coord>, Voxel>(m.CellsFilled, delegate(IEnumerable<Voxel.Coord> coords, Voxel newMap)
					{
						foreach (Voxel.Coord c in coords)
						{
							if (c.Equivalent(attachable.Coord))
							{
								sw.On.Value = c.Data == Voxel.States.PoweredSwitch;
								break;
							}
						}
					});
					entity.Add(cellFilledBinding);

					sw.On.Value = m[attachable.Coord] == Voxel.States.PoweredSwitch;
				}, attachable.AttachedVoxel));
			}

			sw.Add(new Binding<Entity.Handle>(sw.AttachedVoxel, attachable.AttachedVoxel));
			sw.Add(new Binding<Voxel.Coord>(sw.Coord, attachable.Coord));

			this.SetMain(entity, main);

			entity.Add("AttachOffset", attachable.Offset);
			entity.Add("OnPowerOn", sw.OnPowerOn);
			entity.Add("OnPowerOff", sw.OnPowerOff);
			entity.Add("On", sw.On, null, true);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
