﻿(*

Copyright 2013 Jack Pappas

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*)

namespace ExtCore.Caching

open System.Collections.Generic
open LanguagePrimitives
open OptimizedClosures
open ExtCore
open ExtCore.Collections


(* OPTIMIZE :   In the HashMap used in the cache, change the KeyValuePair to a simple struct-tuple
                so we can flip the order the index and value are stored in; this'll provide better
                data alignment in most cases. (E.g., name the type ValueWithKeyIndex) *)
// TODO : Use TagMap instead of IntMap, and tag the key indices with a tag type like KeyIndex.

/// <summary>An immutable cache data structure with a Least-Recently-Used (LRU) eviction policy.</summary>
/// <typeparam name="Key">The type of key used by the cache.</typeparam>
/// <typeparam name="T">The type of the values stored in the cache.</typeparam>
[<Sealed>]
type LruCache<'Key, 'T when 'Key : comparison>
    private (cache : HashMap<'Key, KeyValuePair<int, 'T>>, indexedKeys : IntMap<'Key>,
             capacity : uint32, currentIndex : uint32) =
    /// The empty cache instance.
    static let empty = LruCache (HashMap.empty, IntMap.empty, 0u, 0u)

    /// The empty cache instance.
    static member internal Empty
        with get () = empty

(*
    /// Create an LruCache from a sequence of key-value pairs.
    new (capacity : uint32, elements : seq<'Key * 'T>) =
        // Preconditions
        checkNonNull "elements" elements

        // OPTIMIZATION : When the capacity is zero, we don't even need to consume the sequence.
        if capacity = 0u then
            // Unfortunately, we can't just return the empty cache instance,
            // we must actually call a constructor.
            LruCache (HashMap.empty, IntMap.empty, 0u, 0u)
        else
            // OPTIMIZE : Try to cast the sequence to array or list;
            // if it succeeds use the specialized method for that type for better performance.

            notImpl "LruCache::.ctor(uint32, seq<'Key, 'T>)"
            // TEMP : This is necessary because of the exception being raised (above).
            // It can be removed whenever this code path is implemented.
            LruCache (HashMap.empty, IntMap.empty, 0u, 0u)
*)

    /// Is the cache empty?
    member __.IsEmpty
        with get () =
            IntMap.isEmpty indexedKeys

    /// The maximum number of values which may be stored in the cache.
    member __.Capacity
        with get () = capacity

    /// The number of values stored in the cache.
    member __.Count
        with get () =
            IntMap.count indexedKeys

    /// Look up a key in the cache, returning Some with the associated value if
    /// if the key is in the domain of the cache and None if not. The (possibly)
    /// updated cache is also returned.
    member this.TryFind (key : 'Key) : 'T option * LruCache<'Key, 'T> =
        // Try extracting the key/value from the cache.
        let value, this' = this.Extract key

        // If the key was found (and the value extracted), re-insert the key/value
        // in the cache so the key's index will be updated.
        match value with
        | None ->
            None, this
        | Some keyIndexAndValue ->
            Some keyIndexAndValue.Value,
            this'.Add (key, keyIndexAndValue.Value)

    /// Tests if a key is in the domain of the cache.
    member __.ContainsKey (key : 'Key) : bool =
        HashMap.containsKey key cache

    //
    member this.Add (key : 'Key, value : 'T) : LruCache<'Key, 'T> =
        // OPTIMIZATION : If the capacity is zero (0), we don't need to do anything here.
        if capacity = 0u then this
        else
            // NOTE : It is very important that we use checked addition here with uint32 operands --
            // this data structure currently breaks if the index rolls over from 0xffffffff to 0x00000000.
            let newIndex = Checked.(+) currentIndex 1u
            let newCache = HashMap.add key (KeyValuePair (int newIndex, value)) cache

            let newIndexedKeys =
                // Update the key-index (if necessary, i.e., the key already exists in the cache).
                let newIndexedKeys =
                    match HashMap.tryFind key cache with
                    | None -> indexedKeys
                    | Some keyIndexAndValue ->
                        IntMap.remove keyIndexAndValue.Key indexedKeys

                // Add the key and it's new index to the key-index.
                IntMap.add (int newIndex) key newIndexedKeys

            // Evict the least-recently-used key (and it's associated value) from the cache
            // if this map (the original) is already full.
            if uint32 (IntMap.count newIndexedKeys) > capacity then
                // Remove the minimum key from the key-index and cache.
                // TODO : Fix this so it works correctly even when the index rolls over from 0xffffffff to 0x00000000
                // OPTIMIZE : Use IntMap.extractMin here once it's implemented.
                let minKeyIndex = IntMap.minKey newIndexedKeys
                let minKey = IntMap.find minKeyIndex newIndexedKeys
                let newIndexedKeys = IntMap.remove minKeyIndex newIndexedKeys
                let newCache = HashMap.remove minKey newCache

                // Return a new cache with the updated map and key-index.
                LruCache (newCache, newIndexedKeys, capacity, newIndex)
            else
                // Return a new cache with the updated map and key-index.
                LruCache (newCache, newIndexedKeys, capacity, newIndex)

    //
    member this.Remove (key : 'Key) : LruCache<'Key, 'T> =
        snd <| this.Extract key

    //
    member private this.Extract (key : 'Key) : KeyValuePair<'Key, 'T> option * LruCache<'Key, 'T> =
        match HashMap.tryFind key cache with
        | None ->
            None, this
        | Some keyIndexAndValue ->
            let cache' = HashMap.remove key cache
            let indexedKeys' = IntMap.remove keyIndexAndValue.Key indexedKeys

            // Return the new cache with the modified maps.
            // NOTE : The index is not updated here -- that is only done
            // when adding values to the cache.
            Some (KeyValuePair (key, keyIndexAndValue.Value)),
            LruCache (cache', indexedKeys', capacity, currentIndex)

    //
    member private this.Evict () : LruCache<'Key, 'T> =
        // If the cache is empty, there's nothing to do so just
        // return the cache without modifying it.
        if IntMap.isEmpty indexedKeys then this
        else
            // Remove the minimum key from the key-index and cache.
            // TODO : Fix this so it works correctly even when the index rolls over from 0xffffffff to 0x00000000
            // OPTIMIZE : Use IntMap.extractMin here once it's implemented.
            let minKeyIndex = IntMap.minKey indexedKeys
            let minKey = IntMap.find minKeyIndex indexedKeys
            let newIndexedKeys = IntMap.remove minKeyIndex indexedKeys
            let newCache = HashMap.remove minKey cache

            // Return a new cache with the updated map and key-index.
            LruCache (newCache, newIndexedKeys, capacity, currentIndex)

    //
    member private this.EvictMany (count : uint32) : LruCache<'Key, 'T> =
        if count = 0u then this
        else
            // Evict the next key/value from the cache.
            let this' = this.Evict ()

            // Continue evicting keys until we've evicted the specified number.
            this'.EvictMany (count - 1u)
    
    //
    member this.ChangeCapacity (newCapacity : uint32) : LruCache<'Key, 'T> =
        // If the new capacity is zero (0), return the empty instance.
        if newCapacity = 0u then empty
        elif newCapacity = capacity then
            this        // No change necessary.
        elif newCapacity > capacity then
            // The new capacity is larger than the existing capacity, so we can
            // just copy the data into a new cache with the increased capacity.
            LruCache (cache, indexedKeys, newCapacity, currentIndex)
        else
            // The new capacity is smaller than the old capacity, so we need to
            // evict (capacity - newCapacity) values.
            this.EvictMany (capacity - newCapacity)

    //
    member __.ToSeq () : seq<'Key * 'T> =
        indexedKeys
        |> IntMap.toSeq
        |> Seq.map (fun (_, key) ->
            // Find the value using the key.
            let kvp = HashMap.find key cache
            key, kvp.Value)

    //
    member __.ToList () : ('Key * 'T) list =
        // Fold backwards so we don't have to reverse the created list.
        (indexedKeys, [])
        ||> IntMap.foldBack (fun keyIndex key list ->
            // Find the value using the key.
            let kvp = HashMap.find key cache

            // DEBUG : Assert that the index in the key-value pair is
            // equal to the one from the key-index.
            assert (keyIndex = kvp.Key)

            // Cons the key and value onto the list.
            (key, kvp.Value) :: list)

    //
    member __.ToArray () : ('Key * 'T)[] =
        let kvps = ResizeArray ()

        indexedKeys
        |> IntMap.iter (fun keyIndex key ->
            // Find the value using the key.
            let kvp = HashMap.find key cache

            // DEBUG : Assert that the index in the key-value pair is
            // equal to the one from the key-index.
            assert (keyIndex = kvp.Key)

            // Add the key and value to the ResizeArray
            kvps.Add (key, kvp.Value))

        ResizeArray.toArray kvps

    //
    member __.ToMap () : Map<'Key, 'T> =
        (Map.empty, indexedKeys)
        ||> IntMap.fold (fun map keyIndex key ->
            // Find the value using the key.
            let kvp = HashMap.find key cache

            // DEBUG : Assert that the index in the key-value pair is
            // equal to the one from the key-index.
            assert (keyIndex = kvp.Key)

            // Add the key and value to the map.
            Map.add key kvp.Value map)

    //
    static member internal OfSeq (source : seq<'Key * 'T>, capacity : uint32) : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "source" source

        (LruCache.Create capacity, source)
        ||> Seq.fold (fun cache (key, value) ->
            cache.Add (key, value))

    //
    static member internal OfList (source : ('Key * 'T) list, capacity : uint32) : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "source" source

        (LruCache.Create capacity, source)
        ||> List.fold (fun cache (key, value) ->
            cache.Add (key, value))

    //
    static member internal OfArray (source : ('Key * 'T)[], capacity : uint32) : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "source" source
        
        (LruCache.Create capacity, source)
        ||> Array.fold (fun cache (key, value) ->
            cache.Add (key, value))

    //
    static member internal OfMap (source : Map<'Key, 'T>, capacity : uint32) : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "source" source

        (LruCache.Create capacity, source)
        ||> Map.fold (fun cache key value ->
            cache.Add (key, value))

    /// Create a new LruCache with the specified capacity.
    static member internal Create (capacity : uint32) : LruCache<'Key, 'T> =
        LruCache (HashMap.empty, IntMap.empty, capacity, 0u)

    //
    member __.TryPick (picker : 'Key -> 'T -> 'U option) : 'U option =
        // Adapt the picker for better performance.
        let picker = FSharpFunc<_,_,_>.Adapt picker

        // Traverse the elements in the cache from oldest to newest.
        indexedKeys
        |> IntMap.tryPick (fun _ key ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the picker to the key and value.
            picker.Invoke (key, kvp.Value))

    //
    member this.Pick (picker : 'Key -> 'T -> 'U option) : 'U =
        // Call TryPick, and raise an exception if no value is picked.
        match this.TryPick picker with
        | Some x -> x
        | None ->
            // TODO : Provide a better error message.
            // keyNotFound ""
            raise <| System.Collections.Generic.KeyNotFoundException ()

    //
    member __.Exists (predicate : 'Key -> 'T -> bool) : bool =
        // Adapt the predicate for better performance.
        let predicate = FSharpFunc<_,_,_>.Adapt predicate

        // Traverse the elements in the cache from oldest to newest.
        indexedKeys
        |> IntMap.exists (fun _ key ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the predicate to the key and value.
            predicate.Invoke (key, kvp.Value))

    //
    member __.Forall (predicate : 'Key -> 'T -> bool) : bool =
        // Adapt the predicate for better performance.
        let predicate = FSharpFunc<_,_,_>.Adapt predicate

        // Traverse the elements in the cache from oldest to newest.
        indexedKeys
        |> IntMap.forall (fun _ key ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the predicate to the key and value.
            predicate.Invoke (key, kvp.Value))

    //
    member this.Filter (predicate : 'Key -> 'T -> bool) : LruCache<'Key, 'T> =
        // Adapt the predicate for better performance.
        let predicate = FSharpFunc<_,_,_>.Adapt predicate

        // Remove the keys which don't match the predicate.
        (this, indexedKeys)
        ||> IntMap.fold (fun this _ key ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the predicate to the key and value;
            // if it doesn't match, remove the key.
            if predicate.Invoke (key, kvp.Value) then this
            else this.Remove key)

    //
    member this.Choose (chooser : 'Key -> 'T -> 'U option) : LruCache<'Key, 'U> =
        // Adapt the chooser for better performance.
        let chooser = FSharpFunc<_,_,_>.Adapt chooser

        // Remove the keys which aren't chosen, and create a new HashMap
        // to hold the chosen values.
        let cache, indexedKeys =
            ((HashMap.empty, indexedKeys), indexedKeys)
            ||> IntMap.fold (fun (chosenCache, indexedKeys) _ key ->
                // Get the value from the cache.
                let kvp = HashMap.find key cache

                // Apply the chooser to the key and value;
                // if it doesn't match, remove the key.
                match chooser.Invoke (key, kvp.Value) with
                | None ->
                    let indexedKeys' = IntMap.remove kvp.Key indexedKeys
                    chosenCache, indexedKeys'
                | Some chosenValue ->
                    let chosenCache' =
                        HashMap.add key (KeyValuePair (kvp.Key, chosenValue)) chosenCache
                    chosenCache', indexedKeys)

        // Return a new cache with the updated maps.
        // OPTIMIZE : Only return a new cache if the maps were actually updated.
        LruCache (cache, indexedKeys, capacity, currentIndex)

    /// Apply a function to each key-value pair in the cache, in order from
    /// oldest (least-recently-used) to newest (most-recently-used).
    member this.Iterate (action : 'Key -> 'T -> unit) : unit =
        // Adapt the action for better performance.
        let action = FSharpFunc<_,_,_>.Adapt action

        // Iterate over the keys.
        indexedKeys
        |> IntMap.iter (fun _ key ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the action to the key and value.
            action.Invoke (key, kvp.Value))

    /// Apply a function to each key-value pair in the cache, in order from
    /// newest (most-recently-used) to oldest (least-recently-used).
    member this.IterateBack (action : 'Key -> 'T -> unit) : unit =
        // Adapt the action for better performance.
        let action = FSharpFunc<_,_,_>.Adapt action

        // Iterate over the keys.
        indexedKeys
        |> IntMap.iterBack (fun _ key ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the action to the key and value.
            action.Invoke (key, kvp.Value))

    /// Apply a function to each key-value pair in the cache, in order from
    /// oldest (least-recently-used) to newest (most-recently-used), threading an
    /// accumulator value through the computation.
    member this.Fold (folder : 'State -> 'Key -> 'T -> 'State, state : 'State) : 'State =
        // Adapt the folder for better performance.
        let folder = FSharpFunc<_,_,_,_>.Adapt folder

        // Fold over the keys.
        (state, indexedKeys)
        ||> IntMap.fold (fun state _ key ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the folder to the key and value and the current state value.
            folder.Invoke (state, key, kvp.Value))

    /// Apply a function to each key-value pair in the cache, in order from
    /// newest (most-recently-used) to oldest (least-recently-used), threading an
    /// accumulator value through the computation.
    member this.FoldBack (folder : 'Key -> 'T -> 'State -> 'State, state : 'State) : 'State =
        // Adapt the folder for better performance.
        let folder = FSharpFunc<_,_,_,_>.Adapt folder

        // Fold over the keys.
        (indexedKeys, state)
        ||> IntMap.foldBack (fun _ key state ->
            // Get the value from the cache.
            let kvp = HashMap.find key cache

            // Apply the folder to the key and value and the current state value.
            folder.Invoke (key, kvp.Value, state))

    /// Builds a new cache whose values are the results of applying the given function
    /// to each key-value pair in the cache. The key passed to the function indicates
    /// the key of the element being transformed.
    member this.Map (mapping : 'Key -> 'T -> 'U) : LruCache<'Key, 'U> =
        // Adapt the mapping for better performance.
        let mapping = FSharpFunc<_,_,_>.Adapt mapping

        // We fold over the cache-map (HashMap) here instead of the key-index (IntMap)
        // because it is faster than having to look up the value corresponding to each key.
        // However, this also means the mapping is not applied to the elements in
        // any particular order (oldest-to-newest or newest-to-oldest).
        let mappedCache =
            cache
            |> HashMap.map (fun key (KeyValue (keyIndex, value)) ->
                // Apply the mapping to the key and value.
                let mappedValue = mapping.Invoke (key, value)

                // Return a new KeyValuePair with the key index and mapped value.
                KeyValuePair (keyIndex, mappedValue))

        // Return a new cache with the mapped cache-map.
        LruCache (mappedCache, indexedKeys, capacity, currentIndex)

    //
    member this.Partition (predicate : 'Key -> 'T -> bool) : LruCache<'Key, 'T> * LruCache<'Key, 'T> =
        // Adapt the predicate for better performance.
        let predicate = FSharpFunc<_,_,_>.Adapt predicate

        // We fold over the cache-map (HashMap) here instead of the key-index (IntMap)
        // because it is faster than having to look up the value corresponding to each key.
        // However, this also means the mapping is not applied to the elements in
        // any particular order (oldest-to-newest or newest-to-oldest).
        ((this, this), cache)
        ||> HashMap.fold (fun (trueCache, falseCache) key keyIndexAndValue ->
            // Apply the predicate to the key and value;
            // based on the result, remove the key/value from one of the caches.
            if predicate.Invoke (key, keyIndexAndValue.Value) then
                trueCache,
                falseCache.Remove key
            else
                trueCache.Remove key,
                falseCache)

    //
    member this.MapPartition (partitioner : 'Key -> 'T -> Choice<'U, 'V>) : LruCache<'Key, 'U> * LruCache<'Key, 'V> =
        // Adapt the partitioner for better performance.
        let partitioner = FSharpFunc<_,_,_>.Adapt partitioner

        // We fold over the cache-map (HashMap) here instead of the key-index (IntMap)
        // because it is faster than having to look up the value corresponding to each key.
        // However, this also means the mapping is not applied to the elements in
        // any particular order (oldest-to-newest or newest-to-oldest).
        let cache1, indexedKeys1, cache2, indexedKeys2 =
            ((HashMap.empty, indexedKeys, HashMap.empty, indexedKeys), cache)
            ||> HashMap.fold (fun (cache1, indexedKeys1, cache2, indexedKeys2) key (KeyValue (keyIndex, value)) ->
                // Apply the partitioner to the key and value; based on the result,
                // add the mapped value to one of the caches and remove the key from
                // the opposite key-index.
                match partitioner.Invoke (key, value) with
                | Choice1Of2 result ->
                    HashMap.add key (KeyValuePair (keyIndex, result)) cache1,
                    indexedKeys1,
                    cache2,
                    IntMap.remove keyIndex indexedKeys2
                | Choice2Of2 result ->
                    cache1,
                    IntMap.remove keyIndex indexedKeys1,
                    HashMap.add key (KeyValuePair (keyIndex, result)) cache2,
                    indexedKeys2)

        // Return new caches with the mapped cache-maps and partitioned key-indices.
        LruCache (cache1, indexedKeys1, capacity, currentIndex),
        LruCache (cache2, indexedKeys2, capacity, currentIndex)

//
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LruCache =
    /// The empty cache.
    [<CompiledName("Empty")>]
    let empty<'Key, 'T when 'Key : comparison> =
        LruCache<'Key, 'T>.Empty

    //
    [<CompiledName("Count")>]
    let inline count (cache : LruCache<'Key, 'T>) : int =
        // Preconditions
        checkNonNull "cache" cache

        cache.Count

    //
    [<CompiledName("Capacity")>]
    let inline capacity (cache : LruCache<'Key, 'T>) : uint32 =
        // Preconditions
        checkNonNull "cache" cache

        cache.Capacity

    /// Create a new LruCache with the specified capacity.
    [<CompiledName("Create")>]
    let create (capacity : uint32) : LruCache<'Key, 'T> =
        LruCache.Create capacity

    //
    [<CompiledName("ChangeCapacity")>]
    let inline changeCapacity (cache : LruCache<'Key, 'T>) newCapacity : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache

        cache.ChangeCapacity newCapacity

    //
    [<CompiledName("ContainsKey")>]
    let inline containsKey (key : 'Key) (cache : LruCache<'Key, 'T>) : bool =
        // Preconditions
        checkNonNull "cache" cache

        cache.ContainsKey key

    //
    [<CompiledName("TryFind")>]
    let inline tryFind (key : 'Key) (cache : LruCache<'Key, 'T>) : 'T option * LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache

        cache.TryFind key

    //
    [<CompiledName("Find")>]
    let find (key : 'Key) (cache : LruCache<'Key, 'T>) : 'T * LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache

        let result, cache = tryFind key cache
        match result with
        | None ->
            // TODO : Provide a better error message.
            //keyNotFound ""
            raise <| System.Collections.Generic.KeyNotFoundException ()
        | Some result ->
            result, cache

    //
    [<CompiledName("Add")>]
    let inline add (key : 'Key) (value : 'T) (cache : LruCache<'Key, 'T>) : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache

        cache.Add (key, value)

    //
    [<CompiledName("Remove")>]
    let inline remove (key : 'Key) (cache : LruCache<'Key, 'T>) : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache

        cache.Remove key

    //
    [<CompiledName("OfSeq")>]
    let ofSeq (capacity : uint32) (source : seq<'Key * 'T>) : LruCache<'Key, 'T> =
        // Preconditions checked within the member.
        LruCache.OfSeq (source, capacity)

    //
    [<CompiledName("OfList")>]
    let ofList (capacity : uint32) (source : ('Key * 'T) list) : LruCache<'Key, 'T> =
        // Preconditions checked within the member.
        LruCache.OfList (source, capacity)

    //
    [<CompiledName("OfArray")>]
    let ofArray (capacity : uint32) (source : ('Key * 'T)[]) : LruCache<'Key, 'T> =
        // Preconditions checked within the member.
        LruCache.OfArray (source, capacity)

    //
    [<CompiledName("OfMap")>]
    let ofMap (capacity : uint32) (source : Map<'Key, 'T>) : LruCache<'Key, 'T> =
        // Preconditions checked within the member.
        LruCache.OfMap (source, capacity)

    //
    [<CompiledName("ToSeq")>]
    let inline toSeq (cache : LruCache<'Key, 'T>) : seq<'Key * 'T> =
        // Preconditions
        checkNonNull "cache" cache

        cache.ToSeq ()

    //
    [<CompiledName("ToList")>]
    let inline toList (cache : LruCache<'Key, 'T>) : ('Key * 'T) list =
        // Preconditions
        checkNonNull "cache" cache

        cache.ToList ()

    //
    [<CompiledName("ToArray")>]
    let inline toArray (cache : LruCache<'Key, 'T>) : ('Key * 'T)[] =
        // Preconditions
        checkNonNull "cache" cache

        cache.ToArray ()

    //
    [<CompiledName("ToMap")>]
    let toMap (cache : LruCache<'Key, 'T>) : Map<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache

        cache.ToMap ()

    /// Searches the cache looking for the least-recently-used element where the given function
    /// returns a Some value. If no such element is found, returns None.
    [<CompiledName("TryPick")>]
    let inline tryPick (picker : 'Key -> 'T -> 'U option) (cache : LruCache<'Key, 'T>) : 'U option =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.TryPick picker

    /// Searches the cache looking for the least-recently-used element where the given function
    /// returns a Some value. If no such element is found, KeyNotFoundException is raised.
    [<CompiledName("Pick")>]
    let inline pick (picker : 'Key -> 'T -> 'U option) (cache : LruCache<'Key, 'T>) : 'U =
        // Preconditions
        checkNonNull "cache" cache

        cache.Pick picker

    /// Determines if any binding in the map matches the specified predicate.
    [<CompiledName("Exists")>]
    let inline exists (predicate : 'Key -> 'T -> bool) (cache : LruCache<'Key, 'T>) : bool =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.Exists predicate

    /// Determines if all bindings in the map match the specified predicate.
    [<CompiledName("Forall")>]
    let inline forall (predicate : 'Key -> 'T -> bool) (cache : LruCache<'Key, 'T>) : bool =
        // Preconditions
        checkNonNull "cache" cache

        cache.Forall predicate

    /// <summary>
    /// Builds a new map containing only the bindings for which the given
    /// predicate returns &quot;true&quot;.
    /// </summary>
    [<CompiledName("Filter")>]
    let inline filter (predicate : 'Key -> 'T -> bool) (cache : LruCache<'Key, 'T>) : LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.Filter predicate

    /// <summary>
    /// Applies the given function to each binding in the map.
    /// Returns the map comprised of the results "x" for each binding
    /// where the function returns <c>Some(x)</c>.
    /// </summary>
    [<CompiledName("Choose")>]
    let inline choose (chooser : 'Key -> 'T -> 'U option) (cache : LruCache<'Key, 'T>) : LruCache<'Key, 'U> =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.Choose chooser

    /// Apply a function to each key-value pair in the cache, in order from
    /// oldest (least-recently-used) to newest (most-recently-used).
    [<CompiledName("Iterate")>]
    let inline iter (action : 'Key -> 'T -> unit) (cache : LruCache<'Key, 'T>) : unit =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.Iterate action

    /// Apply a function to each key-value pair in the cache, in order from
    /// newest (most-recently-used) to oldest (least-recently-used).
    [<CompiledName("IterateBack")>]
    let inline iterBack (action : 'Key -> 'T -> unit) (cache : LruCache<'Key, 'T>) : unit =
        // Preconditions
        checkNonNull "cache" cache

        cache.IterateBack action

    /// Apply a function to each key-value pair in the cache, in order from
    /// oldest (least-recently-used) to newest (most-recently-used), threading an
    /// accumulator value through the computation.
    [<CompiledName("Fold")>]
    let inline fold (folder : 'State -> 'Key -> 'T -> 'State) (state : 'State) (cache : LruCache<'Key, 'T>) : 'State =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.Fold (folder, state)

    /// Apply a function to each key-value pair in the cache, in order from
    /// newest (most-recently-used) to oldest (least-recently-used), threading an
    /// accumulator value through the computation.
    [<CompiledName("FoldBack")>]
    let inline foldBack (folder : 'Key -> 'T -> 'State -> 'State) (cache : LruCache<'Key, 'T>) (state : 'State) : 'State =
        // Preconditions
        checkNonNull "cache" cache

        cache.FoldBack (folder, state)

    /// Builds a new cache whose values are the results of applying the given function
    /// to each key-value pair in the cache. The key passed to the function indicates
    /// the key of the element being transformed.
    [<CompiledName("Map")>]
    let inline map (mapping : 'Key -> 'T -> 'U) (cache : LruCache<'Key, 'T>) : LruCache<'Key, 'U> =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.Map mapping

    /// Splits the cache into two caches containing the bindings for which the given
    /// predicate returns true and false, respectively.
    [<CompiledName("Partition")>]
    let inline partition (predicate : 'Key -> 'T -> bool) (cache : LruCache<'Key, 'T>) : LruCache<'Key, 'T> * LruCache<'Key, 'T> =
        // Preconditions
        checkNonNull "cache" cache
        
        cache.Partition predicate

    /// Splits the cache into two caches by applying the given partitioning function
    /// to each binding in the cache.
    [<CompiledName("MapPartition")>]
    let inline mapPartition (partitioner : 'Key -> 'T -> Choice<'U, 'V>) (cache : LruCache<'Key, 'T>) : LruCache<'Key, 'U> * LruCache<'Key, 'V> =
        // Preconditions
        checkNonNull "cache" cache

        cache.MapPartition partitioner
