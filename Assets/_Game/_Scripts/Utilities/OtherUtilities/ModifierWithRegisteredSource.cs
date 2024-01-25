using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Game.daivq.Utilities
{
    [Serializable]
    public class BoolModifierWithRegisteredSource
    {
        private Action _onChanged;
        private List<int> _sources = new();

        public BoolModifierWithRegisteredSource()
        {
        }

        public BoolModifierWithRegisteredSource(Action onChanged)
        {
            _onChanged = onChanged;
        }

        public bool Value { get; private set; }

        public void AddModifier(Object @object)
        {
            AddModifier(@object.GetInstanceID());
        }

        public void AddModifier(int id)
        {
            if (!_sources.Contains(id))
            {
                _sources.Add(id);
                Value = _sources.Count > 0;
                _onChanged?.Invoke();
            }
        }

        public void RemoveModifier(Object @object)
        {
            RemoveModifier(@object.GetInstanceID());
        }

        public void RemoveModifier(int id)
        {
            _sources.Remove(id);
            Value = _sources.Count > 0;
            _onChanged?.Invoke();
        }
    }

    public class Vector2AddModifierWithRegisteredSource
    {
        protected Dictionary<int, Vector2> _source = new();
        protected Vector2 _value = Vector2.zero;
        public Vector2 Value => _value;

        public void AddModifier(Object @object, Vector2 value)
        {
            AddModifier(@object.GetInstanceID(), value);
        }

        public void AddModifier(int id, Vector2 value)
        {
            if (_source.ContainsKey(id))
                _source[id] = value;
            else
                _source.Add(id, value);
            UpdateModifier();
        }

        public void RemoveModifier(Object @object)
        {
            RemoveModifier(@object.GetInstanceID());
        }

        public void RemoveModifier(int id)
        {
            if (_source.Remove(id)) UpdateModifier();
        }

        private void UpdateModifier()
        {
            _value = Vector2.zero;
            foreach (Vector2 value in _source.Values) _value += value;
        }
    }

    public abstract class FloatModifierWithRegisteredSource
    {
        protected Dictionary<int, float> _source = new();
        protected float _value;

        protected FloatModifierWithRegisteredSource()
        {
            _value = InitValue;
        }

        public float Value => _value;

        protected abstract float InitValue { get; }

        public void AddModifier(Object @object, float value)
        {
            AddModifier(@object.GetInstanceID(), value);
        }

        public void AddModifier(int id, float value)
        {
            if (_source.ContainsKey(id))
                _source[id] = value;
            else
                _source.Add(id, value);
            UpdateModifier();
        }

        public void RemoveModifier(Object @object)
        {
            RemoveModifier(@object.GetInstanceID());
        }

        public void RemoveModifier(int id)
        {
            if (_source.Remove(id)) UpdateModifier();
        }

        protected abstract void UpdateModifier();
    }

    [Serializable]
    public class FloatMulModifierWithRegisteredSource : FloatModifierWithRegisteredSource
    {
        protected override float InitValue => 1f;

        protected override void UpdateModifier()
        {
            _value = 1f;
            foreach (float value in _source.Values) _value *= value;
        }
    }

    [Serializable]
    public class FloatAddModifierWithRegisteredSource : FloatModifierWithRegisteredSource
    {
        protected override float InitValue => 0f;

        protected override void UpdateModifier()
        {
            _value = 0f;
            foreach (float value in _source.Values) _value += value;
        }
    }

    [Serializable]
    public class FloatMinModifierWithRegisteredSource : FloatModifierWithRegisteredSource
    {
        private float _init;

        public FloatMinModifierWithRegisteredSource(float init = float.MaxValue)
        {
            _value = _init = init;
        }

        protected override float InitValue => _init;

        protected override void UpdateModifier()
        {
            _value = _init;
            foreach (float value in _source.Values)
                if (value < _value)
                    _value = value;
        }
    }

    [Serializable]
    public class FloatMaxModifierWithRegisteredSource : FloatModifierWithRegisteredSource
    {
        private float _init;

        public FloatMaxModifierWithRegisteredSource(float init = float.MinValue)
        {
            _value = _init = init;
        }

        protected override float InitValue => _init;

        protected override void UpdateModifier()
        {
            _value = _init;
            foreach (float value in _source.Values)
                if (value > _value)
                    _value = value;
        }
    }

    [Serializable]
    public class ComponentOverrideWithRegisteredSource<T> where T : Component
    {
        private Action _onChangedValueHighestPriority;

        protected Dictionary<int, Data> _source = new();

        public ComponentOverrideWithRegisteredSource()
        {
        }

        public ComponentOverrideWithRegisteredSource(Action onChanged)
        {
            _onChangedValueHighestPriority = onChanged;
        }

        public T ValueHighestPriority { get; private set; }

        public void AddModifier(Object @object, T component, float priority)
        {
            AddModifier(@object.GetInstanceID(), component, priority);
        }

        public void AddModifier(int id, T component, float priority)
        {
            if (_source.ContainsKey(id))
                _source[id] = new Data { component = component, priority = priority };
            else
                _source.Add(id, new Data { component = component, priority = priority });
            UpdateModifier();
        }

        public void RemoveModifier(Object @object)
        {
            RemoveModifier(@object.GetInstanceID());
        }

        public void RemoveModifier(int id)
        {
            if (_source.Remove(id)) UpdateModifier();
        }

        private void UpdateModifier()
        {
            T componentMaxPriority = null;
            float maxPriority = float.MinValue;
            foreach (KeyValuePair<int, Data> pair in _source)
                if (pair.Value.priority > maxPriority)
                {
                    maxPriority = pair.Value.priority;
                    componentMaxPriority = pair.Value.component;
                }

            if (ValueHighestPriority != componentMaxPriority)
            {
                ValueHighestPriority = componentMaxPriority;
                _onChangedValueHighestPriority?.Invoke();
            }
        }

        [Serializable]
        public struct Data
        {
            public T component;
            public float priority;
        }
    }

    [Serializable]
    public class ModifierFloatCountdown
    {
        private MonoBehaviour _context;
        private Dictionary<int, Coroutine> _corCountdowns = new();
        private FloatModifierWithRegisteredSource _modifier;

        public ModifierFloatCountdown(MonoBehaviour context, FloatModifierWithRegisteredSource modifier)
        {
            _context = context;
            _modifier = modifier;
        }

        public void Add(int source, float value, float duration)
        {
            _modifier.AddModifier(source, value);
            if (_corCountdowns.TryGetValue(source, out Coroutine cor))
            {
                _context.StopCoroutine(cor);
                _corCountdowns[source] = _context.StartCoroutine(IECountdown(source, duration));
            }
            else
            {
                _corCountdowns.Add(source, _context.StartCoroutine(IECountdown(source, duration)));
            }
        }

        private IEnumerator IECountdown(int source, float duration)
        {
            float timeEnd = Time.time + duration;
            while (Time.time < timeEnd) yield return null;
            _corCountdowns.Remove(source);
            _modifier.RemoveModifier(source);
        }
    }

    [Serializable]
    public class ModifierBoolCountdown
    {
        private MonoBehaviour _context;
        private Dictionary<int, Coroutine> _corCountdowns = new();
        private BoolModifierWithRegisteredSource _modifier;

        public ModifierBoolCountdown(MonoBehaviour context, BoolModifierWithRegisteredSource modifier)
        {
            _context = context;
            _modifier = modifier;
        }

        public void Add(int source, float duration)
        {
            _modifier.AddModifier(source);
            if (_corCountdowns.TryGetValue(source, out Coroutine cor))
            {
                _context.StopCoroutine(cor);
                _corCountdowns[source] = _context.StartCoroutine(IECountdown(source, duration));
            }
            else
            {
                _corCountdowns.Add(source, _context.StartCoroutine(IECountdown(source, duration)));
            }
        }

        private IEnumerator IECountdown(int source, float duration)
        {
            float timeEnd = Time.time + duration;
            while (Time.time < timeEnd) yield return null;
            _corCountdowns.Remove(source);
            _modifier.RemoveModifier(source);
        }
    }
}
