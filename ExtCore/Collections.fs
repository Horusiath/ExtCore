﻿(*

Copyright 2010-2012 TidePowerd Ltd.
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

//
namespace ExtCore.Collections

open LanguagePrimitives
open OptimizedClosures
open ExtCore


//
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ResizeArray =
    open System.Collections.Generic


    //
    let [<NoDynamicInvocation>] inline length (resizeArray : ResizeArray<'T>) =
        resizeArray.Count

    //
    let [<NoDynamicInvocation>] inline add item (resizeArray : ResizeArray<'T>) =
        resizeArray.Add item

    //
    let [<NoDynamicInvocation>] inline toArray (resizeArray : ResizeArray<'T>) =
        resizeArray.ToArray ()

    //
    let [<NoDynamicInvocation>] inline ofArray (arr : 'T[]) : ResizeArray<'T> =
        ResizeArray (arr)

    //
    let [<NoDynamicInvocation>] inline ofSeq (sequence : seq<'T>) : ResizeArray<'T> =
        ResizeArray (sequence)

    //
    let [<NoDynamicInvocation>] inline get (resizeArray : ResizeArray<'T>) index =
        resizeArray.[index]

    //
    let [<NoDynamicInvocation>] inline set (resizeArray : ResizeArray<'T>) index value =
        resizeArray.[index] <- value

    //
    let [<NoDynamicInvocation>] inline sortInPlace<'T when 'T : comparison> (resizeArray : ResizeArray<'T>) =
        resizeArray.Sort ()

    //
    let [<NoDynamicInvocation>] inline sortInPlaceBy<'T, 'Key when 'Key : comparison>
            (projection : 'T -> 'Key) (resizeArray : ResizeArray<'T>) =
        resizeArray.Sort (fun x y ->
            compare (projection x) (projection y))

    //
    let [<NoDynamicInvocation>] inline sortInPlaceWith (comparer : 'T -> 'T -> int) (resizeArray : ResizeArray<'T>) =
        resizeArray.Sort (comparer)

    // TODO:
    // map, mapi
    // iter, iteri
    // fold, foldBack
    // reduce, reduceBack
    // exists, forall
    // find, tryFind
    // findIndex, tryFindIndex
    // pick, tryPick
    // choose
    // singleton


/// Functional programming operators related to the System.Collections.Generic.IDictionary type.
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Dict =
    open System.Collections.Generic

    /// Thread-safe functional programming operators for mutable instances of System.Collections.Generic.IDictionary.
    module Safe =
        /// Attempts to retrieve the value associated with the specified key.
        let tryFind k (d : IDictionary<'Key, 'T>) =
            match lock d <| fun () -> d.TryGetValue k with
            | false, _ -> None
            | true, v -> Some v
        
        /// Lookup an element in the Dictionary, raising KeyNotFoundException if
        /// the Dictionary does not contain an element with the specified key.
        let find k (d : IDictionary<'Key, 'T>) =
            lock d <| fun () -> d.[k]

        /// Adds a new entry to the Dictionary.
        let add k v (d : IDictionary<'Key, 'T>) =
            if d.IsReadOnly then
                invalidOp "Cannot add an entry to a read-only dictionary."
            lock d <| fun () ->
                d.Add (k, v)

        /// Updates an existing entry in the dictionary with a new value,
        /// raising KeyNotFoundException if the Dictionary does not
        /// contain an element with the specified key.
        let update k v (d : IDictionary<'Key, 'T>) =
            if d.IsReadOnly then
                invalidOp "Cannot update an entry in a read-only dictionary."
            lock d <| fun () ->
                if d.ContainsKey k then
                    d.[k] <- v
                else
                    raise <| System.Collections.Generic.KeyNotFoundException ()

        /// Removes the entry with the specified key from the Dictionary,
        /// returning a value indicating the success of the operation.
        let remove (k : 'Key) (d : IDictionary<'Key, 'T>) =
            if d.IsReadOnly then
                invalidOp "Cannot remove an entry from a read-only dictionary."
            lock d <| fun () ->
                d.Remove k

        /// Updates the value of an entry (which has the specified key)
        /// in the Dictionary, or creates a new entry if one doesn't exist.
        /// If the Dictionary is read-only, an InvalidOperationException is raised.
        let updateOrAdd k v (d : IDictionary<'Key, 'T>) =
            if d.IsReadOnly then
                invalidOp "Cannot update or add an entry to a read-only dictionary."
            lock d <| fun () ->
                d.[k] <- v

        /// Creates an immutable copy of a Dictionary.
        /// The entries are shallow-copied to the created Dictionary; that is,
        /// reference-typed keys and values will reference the same instances as
        /// in the mutable Dictionary, so care should be taken when using mutable keys and values.
        let toImmutable (d : IDictionary<'Key, 'T>) =
            lock d <| fun () ->
                d
                |> Seq.map (fun kvp ->
                    kvp.Key, kvp.Value)
                |> dict


    /// Views the keys of the Dictionary as a sequence.
    let [<NoDynamicInvocation>] inline keys (dictionary : IDictionary<'Key, 'T>) =
        dictionary.Keys :> IEnumerable<'Key>

    /// Views the values of the Dictionary as a sequence.
    let [<NoDynamicInvocation>] inline values (dictionary : IDictionary<'Key, 'T>) =
        dictionary.Values :> IEnumerable<'T>

    /// Determines whether the Dictionary is empty.
    let [<NoDynamicInvocation>] inline isEmpty (dictionary : IDictionary<'Key,'T>) =
        dictionary.Count = 0

    /// Gets the number of entries in the Dictionary.
    let [<NoDynamicInvocation>] inline count (dictionary : IDictionary<'Key, 'T>) =
        dictionary.Count

    /// Gets the number of entries in the Dictionary as an unsigned integer.
    let [<NoDynamicInvocation>] inline natCount (dictionary : IDictionary<'Key, 'T>) =
        Checked.uint32 dictionary.Count

    /// Creates a mutable dictionary with the specified capacity.
    let [<NoDynamicInvocation>] inline createMutable<'Key, 'T when 'Key : equality> (capacity : int) =
        System.Collections.Generic.Dictionary<'Key, 'T> (capacity)

    /// Determines whether the Dictionary contains an element with the specified key.
    let [<NoDynamicInvocation>] inline containsKey k (dictionary : IDictionary<'Key, 'T>) =
        dictionary.ContainsKey k

    /// Adds a new entry to the dictionary.
    let [<NoDynamicInvocation>] inline add k v (dictionary : IDictionary<'Key, 'T>) =
        dictionary.Add (k, v)
        dictionary

    /// Removes the entry with the specified key from the Dictionary.
    /// An exception is raised if the entry cannot be removed.
    let [<NoDynamicInvocation>] inline remove (k : 'Key) (dictionary : IDictionary<'Key, 'T>) =
        if dictionary.Remove k then dictionary
        else failwithf "Unable to remove the entry with the key '%O' from the dictionary." k

    /// Lookup an element in the Dictionary, raising KeyNotFoundException if
    /// the dictionary does not contain an element with the specified key.
    let [<NoDynamicInvocation>] inline find k (dictionary : IDictionary<'Key, 'T>) =
        dictionary.[k]
            
    /// Attempts to retrieve the value associated with the specified key.
    let [<NoDynamicInvocation>] inline tryFind k (dictionary : IDictionary<'Key, 'T>) =
        match dictionary.TryGetValue k with
        | false, _ -> None
        | true, v -> Some v

    /// Updates the value of an entry (which has the specified key) in the Dictionary.
    /// Raises a KeyNotFoundException if the Dictionary does not contain an entry with the specified key.
    let update k v (dictionary : IDictionary<'Key, 'T>) =
        if dictionary.ContainsKey k then
            dictionary.[k] <- v
            dictionary
        else
            // TODO : Add an error message which includes the key.
            raise <| System.Collections.Generic.KeyNotFoundException ()

    /// Updates the value of an entry (which has the specified key) in
    /// the Dictionary, or creates a new entry if one doesn't exist.
    let [<NoDynamicInvocation>] inline updateOrAdd k v (dictionary : IDictionary<'Key, 'T>) =
        dictionary.[k] <- v
        dictionary    

    /// Applies the given function to successive entries, returning the
    /// first result where the function returns "Some(x)".
    let [<NoDynamicInvocation>] inline tryPick f (dictionary : IDictionary<'Key, 'T>) =
        dictionary
        |> Seq.tryPick (fun kvp ->
            f kvp.Key kvp.Value)

    /// Applies the given function to sucecssive entries, returning the
    /// first x where the function returns "Some(x)".
    let [<NoDynamicInvocation>] inline pick f (dictionary : IDictionary<'Key, 'T>) =
        match tryPick f dictionary with
        | Some res -> res
        | None ->
            // TODO : Add an error message which includes the key.
            raise <| System.Collections.Generic.KeyNotFoundException ()

    /// Views the Dictionary as a sequence of tuples.
    let toSeq (dictionary : IDictionary<'Key, 'T>) =
        dictionary
        |> Seq.map (fun kvp ->
            kvp.Key, kvp.Value)

    /// Applies the given function to each entry in the Dictionary.
    let iter (action : 'Key -> 'T -> unit) (dictionary : IDictionary<'Key, 'T>) =
        dictionary
        |> Seq.iter (fun kvp ->
            action kvp.Key kvp.Value)

    /// Returns a new Dictionary containing only the entries of the
    /// Dictionary for which the predicate returns 'true'.
    let filter (predicate : 'Key -> 'T -> bool) (dictionary : IDictionary<'Key, 'T>) =
        dictionary
        |> Seq.choose (fun kvp ->
            if predicate kvp.Key kvp.Value then
                Some (kvp.Key, kvp.Value)
            else None)
        |> dict

    /// Builds a new Dictionary whose entries are the results of applying
    /// the given function to each element of the Dictionary.
    let map (f : 'Key -> 'T -> 'U) (dictionary : IDictionary<'Key, 'T>) =
        dictionary
        |> Seq.map (fun kvp ->
            kvp.Key, f kvp.Key kvp.Value)
        |> dict

    /// Applies the given function to each element of the Dictionary.
    /// Returns a Dictionary comprised of the results "x,y" for each
    /// entry where the function returns Some(y).
    let choose (f : 'Key -> 'T -> 'U option) (dictionary : IDictionary<'Key, 'T>) =
        dictionary
        |> Seq.choose (fun kvp ->
            f kvp.Key kvp.Value
            |> Option.map (fun x -> kvp.Key, x))
        |> dict

    /// Applies a function to each entry of the Dictionary,
    /// threading an accumulator argument through the computation.
    let fold f (state : 'State) (dictionary : IDictionary<'Key, 'T>) =
        (state, dictionary)
        ||> Seq.fold (fun state kvp ->
            f state kvp.Key kvp.Value)

    /// Splits the Dictionary into two Dictionaries, containing the entries
    /// for which the given predicate evaluates to "true" and "false".
    let partition p (dictionary : IDictionary<'Key, 'T>) =
        let t, f =
            ((Seq.empty, Seq.empty), dictionary)
            ||> fold (fun (trueSeq, falseSeq) k v ->
                if p k v then
                    (Seq.append trueSeq (Seq.singleton (k, v)), falseSeq)
                else
                    (trueSeq, Seq.append falseSeq (Seq.singleton (k, v))))

        dict t, dict f

    //
    let readonly (dictionary : IDictionary<'Key, 'T>) =
        dict <| toSeq dictionary


/// Additional functional operators on sequences.
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Seq =
    /// Returns the length of the sequence as an unsigned integer.
    let [<NoDynamicInvocation>] inline natLength s =
        uint32 <| Seq.length s

    //
    let [<NoDynamicInvocation>] inline appendSingleton s el =
        Seq.append s (Seq.singleton el)

    //
    let project (mapping : 'T -> 'U) (source : seq<'T>) =
        source
        |> Seq.map (fun x ->
            x, mapping x)


/// Additional functional operators on immutable lists.
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module List =
    /// A curried "cons" operator.
    let [<NoDynamicInvocation>] inline cons (list : 'T list) value =
        value :: list

    /// A curried "optional-cons" operator.
    let [<NoDynamicInvocation>] inline optcons (list : 'T list) value =
        match value with
        | None -> list
        | Some x -> x :: list

    /// Returns the length of the list as an unsigned integer.
    let [<NoDynamicInvocation>] inline natLength (list : 'T list) =
        uint32 <| List.length list

    //
    let [<NoDynamicInvocation>] inline tryHead (list : 'T list) =
        match list with
        | [] -> None
        | hd :: _ ->
            Some hd

    //
    let [<NoDynamicInvocation>] inline ofOption (value : 'T option) =
        match value with
        | None -> []
        | Some x -> [x]

    //
    let [<NoDynamicInvocation>] inline singleton (value : 'T) =
        [value]

    /// Converts a list into an array (similar to List.toArray) but copies the elements into
    /// the array from right-to-left, so there's no need to call List.rev before List.toArray.
    [<CompiledName("ReverseIntoArray")>]
    let revIntoArray (list : 'T list) =
        checkNonNull "list" list

        let len = List.length list
        let results = Array.zeroCreate len

        let rec loop (idx, lst) =
            match lst with
            | [] -> results
            | hd :: tl ->
                results.[idx] <- hd
                loop (idx - 1, tl)

        loop (len - 1, list)

    /// <summary>Applies the given function to each element of a list,
    /// and copies the results into an array from right-to-left so the
    /// produced array represents the mapped original list in reverse order.</summary>
    /// <remarks><para>This represents an optimized version of:
    /// <c>fun mapping -> (List.map mapping) >> List.rev >> List.toArray</c>.</para></remarks>
    [<CompiledName("MapAndReverseIntoArray")>]
    let mapAndRevIntoArray (mapping : 'T -> 'U) (list : 'T list) =
        checkNonNull "list" list

        let len = List.length list
        let results = Array.zeroCreate len

        let rec loop (idx, lst) =
            match lst with
            | [] -> results
            | hd :: tl ->
                results.[idx] <- mapping hd
                loop (idx - 1, tl)

        loop (len - 1, list)

    //
    [<CompiledName("ProjectValues")>]
    let projectValues (projection : 'Key -> 'T) (list : 'Key list) =
        list |> List.map (fun x -> x, projection x)

    //
    [<CompiledName("ProjectKeys")>]
    let projectKeys (projection : 'T -> 'Key) (list : 'T list) =
        list |> List.map (fun x -> projection x, x)

    /// Takes a specified number of items from a list, returning them (as a new list) along with the remaining list.
    [<CompiledName("Take")>]
    let take count (list : 'T list) =
        // Preconditions
        if count < 0 then
            invalidArg "count" "The number of items to take from the list is negative."
        checkNonNull "list" list
        if count > List.length list then
            invalidArg "count" "The number of items to take from the list is greater than the length of the list."

        // OPTIMIZATION : If count = 0 return immediately.
        if count = 0 then
            [], list
        else
            /// The result list.
            let mutable taken = []
            let mutable list = list
            
            // Take the elements from the input list and cons them onto the 'taken' list.
            for i = 0 to count - 1 do
                taken <- (List.head list) :: taken
                list <- List.tail list

            // Return the 'taken' list and the remaining part of the list.
            // Reverse the 'taken' list so it's in the correct order.
            List.rev taken, list

    /// Takes a specified number of items from a list, returning them (in an array) along with the remaining list.
    [<CompiledName("TakeArray")>]
    let takeArray count (list : 'T list) =
        // Preconditions
        if count < 0 then
            invalidArg "count" "The number of items to take from the list is negative."
        checkNonNull "list" list
        if count > List.length list then
            invalidArg "count" "The number of items to take from the list is greater than the length of the list."

        // OPTIMIZATION : If count = 0 return immediately.
        if count = 0 then
            Array.empty, list
        else
            /// The result array.
            let takenElements = Array.zeroCreate count

            let mutable list = list
            
            // Take the elements from the list and store them in the array.
            for i = 0 to count - 1 do
                takenElements.[i] <- List.head list
                list <- List.tail list

            // Return the taken elements and the remaining part of the list.
            takenElements, list

    //
    [<CompiledName("FoldPairs")>]
    let foldPairs (folder : 'State -> 'T -> 'T -> 'State) state list =
        // Preconditions
        checkNonNull "list" list

        // OPTIMIZATION : If the list is empty or contains just one element,
        // immediately return the input state.
        match list with
        | []
        | [_] ->
            state
        | hd :: tl ->
            // OPTIMIZATION : Imperative-style implementation for maximum performance.            
            let folder = FSharpFunc<_,_,_,_>.Adapt folder
            let mutable previousElement = hd
            let mutable list = tl
            let mutable state = state

            while not <| List.isEmpty list do
                let currentElement = List.head list
                state <- folder.Invoke (state, previousElement, currentElement)
                previousElement <- currentElement
                list <- List.tail list

            // Return the final state value
            state

    //
    [<CompiledName("FoldPairsBack")>]
    let foldPairsBack (folder : 'T -> 'T -> 'State -> 'State) state list =
        // Preconditions
        checkNonNull "list" list

        // OPTIMIZATION : If the list is empty or contains just one element,
        // immediately return the input state.
        match list with
        | []
        | [_] ->
            state
        | list ->
            let folder = FSharpFunc<_,_,_,_>.Adapt folder
            // OPTIMIZATION : To fold backwards over a single-linked list, we normally need to traverse
            // it and create a reversed copy -- this means O(n) time and memory complexity.
            // Here, we squeeze out a bit more performance by using an array to hold the reversed list
            // so we benefit from memory locality.
            let reversed = revIntoArray list
            let mutable state = state
            
            let len = Array.length reversed
            for i = 1 to len - 1 do
                state <- folder.Invoke (reversed.[i - 1], reversed.[i], state)

            // Return the final state value
            state

    //
    [<CompiledName("MapPartition")>]
    let mapPartition (partitioner : 'T -> Choice<'U1, 'U2>) list : 'U1 list * 'U2 list =
        // Preconditions
        checkNonNull "list" list

        // OPTIMIZATION : If the input list is empty, immediately return empty results.
        if List.isEmpty list then
            [], []
        else
            // Mutable variables are used here instead of List.fold for maximum performance.
            let mutable list = list
            let mutable resultList1 = []
            let mutable resultList2 = []

            // Partition the list, consing the elements onto the list
            // specified by the partition function.
            while not <| List.isEmpty list do
                match partitioner <| List.head list with
                | Choice1Of2 element ->
                    resultList1 <- element :: resultList1
                | Choice2Of2 element ->
                    resultList2 <- element :: resultList2

                // Remove the first element from the input list.
                list <- List.tail list

            // Reverse the result lists and return them.
            List.rev resultList1,
            List.rev resultList2

    //
    [<CompiledName("MapPartition3")>]
    let mapPartition3 (partitioner : 'T -> Choice<'U1, 'U2, 'U3>) list : 'U1 list * 'U2 list * 'U3 list =
        // Preconditions
        checkNonNull "list" list

        // OPTIMIZATION : If the input list is empty, immediately return empty results.
        if List.isEmpty list then
            [], [], []
        else
            // Mutable variables are used here instead of List.fold for maximum performance.
            let mutable list = list
            let mutable resultList1 = []
            let mutable resultList2 = []
            let mutable resultList3 = []

            // Partition the list, consing the elements onto the list
            // specified by the partition function.
            while not <| List.isEmpty list do
                match partitioner <| List.head list with
                | Choice1Of3 element ->
                    resultList1 <- element :: resultList1
                | Choice2Of3 element ->
                    resultList2 <- element :: resultList2
                | Choice3Of3 element ->
                    resultList3 <- element :: resultList3

                // Remove the first element from the input list.
                list <- List.tail list

            // Reverse the result lists and return them.
            List.rev resultList1,
            List.rev resultList2,
            List.rev resultList3

    //
    [<CompiledName("Choose2")>]
    let choose2 (chooser : 'T1 -> 'T2 -> 'U option) list1 list2 : 'U list =
        // Preconditions
        checkNonNull "list1" list1
        checkNonNull "list2" list2
        // OPTIMIZE : Instead of checking List.length on both lists (which is O(n)),
        // just detect mismatched lengths on-the-fly.
        if List.length list1 <> List.length list2 then
            invalidArg "list2" "The lists have different lengths."

        let chooser = FSharpFunc<_,_,_>.Adapt chooser

        let mutable list1 = list1
        let mutable list2 = list2
        let mutable resultList = []

        while not <| List.isEmpty list1 do
            match chooser.Invoke (List.head list1, List.head list2) with
            | None -> ()
            | Some result ->
                resultList <- result :: resultList

            list1 <- List.tail list1
            list2 <- List.tail list2

        // Reverse the result list before returning.
        List.rev resultList

    //
    [<CompiledName("Unfold")>]
    let unfold (generator : 'State -> ('T * 'State) option) (state : 'State) : 'T list =
        let mutable resultList = []
        let mutable state = state
        let mutable finished = false

        // Generate elements and cons them onto the result list.
        while not finished do
            match generator state with
            | Some (result, state') ->
                resultList <- result :: resultList
                state <- state'
            | None ->
                finished <- true

        // Reverse the result list before returning.
        List.rev resultList

    //
    [<CompiledName("ZipWith")>]
    let zipWith (mapping : 'T1 -> 'T2 -> 'U) list1 list2 : 'U list =
        // Preconditions
        checkNonNull "list1" list1
        checkNonNull "list2" list2
        // OPTIMIZE : Instead of checking List.length on both lists (which is O(n)),
        // just detect mismatched lengths on-the-fly.
        if List.length list1 <> List.length list2 then
            invalidArg "list2" "The lists have different lengths."

        let mapping = FSharpFunc<_,_,_>.Adapt mapping
        
        let mutable list1 = list1
        let mutable list2 = list2
        let mutable resultList = []

        while not <| List.isEmpty list1 do
            resultList <-
                mapping.Invoke (List.head list1, List.head list2) :: resultList
            list1 <- List.tail list1
            list2 <- List.tail list2

        // Reverse the result list before returning.
        List.rev resultList

    //
    [<CompiledName("UnzipWith")>]
    let unzipWith (mapping : 'T -> 'U * 'V) list : 'U list * 'V list =
        // Preconditions
        checkNonNull "list" list

        // OPTIMIZATION : If the input list is empty return immediately.
        if List.isEmpty list then
            [], []
        else
            let mutable list = list
            let mutable resultList1 = []
            let mutable resultList2 = []

            while not <| List.isEmpty list do
                let result1, result2 =
                    mapping <| List.head list
                list <- List.tail list
                resultList1 <- result1 :: resultList1
                resultList2 <- result2 :: resultList2

            // Reverse the result lists before returning.
            List.rev resultList1,
            List.rev resultList2


/// Additional functional operators on arrays.
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Array =
    /// Returns the length of the array as an unsigned integer.
    let [<NoDynamicInvocation>] inline natLength (arr : 'T[]) =
        uint32 arr.Length

    //
    let [<NoDynamicInvocation>] inline singleton (value : 'T) =
        [| value |]

    /// Wraps the array in a ReadOnlyCollection<'T> to only allow
    /// read access while still providing O(n) lookup.
    let [<NoDynamicInvocation>] inline readonly (arr : 'T[]) =
        System.Array.AsReadOnly arr

    /// Applies a function to each element of the collection, threading an accumulator argument through the computation.
    /// The integer index passed to the function indicates the array index of the element being transformed.
    [<CompiledName("FoldIndexed")>]
    let foldi (folder : int -> 'State -> 'T -> 'State) (state : 'State) (arr : 'T[]) =
        checkNonNull "arr" arr
        let folder = FSharpFunc<_,_,_,_>.Adapt folder
        let mutable state = state
        let len = arr.Length
        for i = 0 to len - 1 do
            state <- folder.Invoke (i, state, arr.[i])
        state

    /// Applies a function to each element of the array, returning a new array whose elements are
    /// tuples of the original element and the function result for that element.
    [<CompiledName("ProjectValues")>]
    let projectValues (projection : 'Key -> 'U) (array : 'Key[]) =
        array |> Array.map (fun x -> x, projection x)

    //
    [<CompiledName("ProjectKeys")>]
    let projectKeys (projection : 'T -> 'Key) (array : 'T[]) =
        array |> Array.map (fun x -> projection x, x)

    /// Returns the first element in the array.
    let [<NoDynamicInvocation>] inline first (arr : 'T[]) =
        if Array.isEmpty arr then
            invalidOp "Cannot retrieve the first element of an empty array."
        else arr.[0]

    /// Returns the index of the last element in the array.
    let [<NoDynamicInvocation>] inline lastIndex (arr : 'T[]) =
        if Array.isEmpty arr then
            invalidOp "The array is empty."
        else arr.Length - 1

    /// Returns the last element in the array.
    let [<NoDynamicInvocation>] inline last (arr : 'T[]) =
        if Array.isEmpty arr then
            invalidOp "Cannot retrieve the last element of an empty array."
        else arr.[arr.Length - 1]

    /// Splits an array into one or more arrays; the specified predicate is applied
    /// to each element in the array, and whenever it returns true, that element will
    /// be the first element in one of the "subarrays".
    [<CompiledName("Split")>]
    let split (predicate : 'T -> bool) (array : 'T[]) =
        checkNonNull "array" array

        let segments = ResizeArray<_> ()
        let mutable currentSegment = ResizeArray<_> ()

        let len = array.Length
        for i = 0 to len - 1 do
            let el = array.[i]
            if currentSegment.Count > 0 && predicate el then
                segments.Add <| currentSegment.ToArray ()
                currentSegment <- ResizeArray<_> ()

            currentSegment.Add el

        // Append the last segment to the segment list,
        // then return the segment list as an array.
        segments.Add <| currentSegment.ToArray ()
        segments.ToArray ()

    /// Splits an array into one or more segments by applying the specified predicate
    /// to each element of the array and starting a new segment at each element where
    /// the predicate returns true.
    [<CompiledName("Segment")>]
    let segment (predicate : 'T -> bool) (array : 'T[]) =
        checkNonNull "array" array
        
        let segments = ResizeArray<_> ()

        let len = Array.length array
        let mutable segmentLength = 0
        for i = 0 to len - 1 do
            //
            if segmentLength > 0 && predicate array.[i] then
                // NOTE : The current element is the first element in the *new* segment!
                let offset = i - segmentLength

                System.ArraySegment<_> (array, offset, segmentLength)
                |> segments.Add

                segmentLength <- 1
            else
                // The existing segment is empty, or the predicate returned false --
                // so "append" the element to the existing array segment.
                segmentLength <- segmentLength + 1

        // Finish the last/current segment, then return the list of segments as an array.
        let offset = len - segmentLength

        System.ArraySegment<_> (array, offset, segmentLength)
        |> segments.Add

        segments.ToArray()

    /// Splits two arrays into one or more segments by applying the specified predicate
    /// to the each pair of array elements and starting a new segment whenever the
    /// predicate returns true.
    [<CompiledName("Segment2")>]
    let segment2 (predicate : 'T -> 'U -> bool) (array1 : 'T[]) (array2 : 'U[]) =
        checkNonNull "array1" array1
        checkNonNull "array2" array2
        let predicate = FSharpFunc<_,_,_>.Adapt predicate
        let len1 = array1.Length 
        if len1 <> array2.Length then
            invalidArg "array2" "The arrays have differing lengths."

        let segments1 = ResizeArray<_> ()
        let segments2 = ResizeArray<_> ()

        let mutable segmentLength = 0
        for i = 0 to len1 - 1 do
            //
            if segmentLength > 0 && predicate.Invoke (array1.[i], array2.[i]) then
                // NOTE : The current element is the first element in the *new* segment!
                let offset = i - segmentLength

                System.ArraySegment<_> (array1, offset, segmentLength)
                |> segments1.Add
                System.ArraySegment<_> (array2, offset, segmentLength)
                |> segments2.Add

                segmentLength <- 1
            else
                // The existing segment is empty, or the predicate returned false --
                // so "append" the element to the existing array segment.
                segmentLength <- segmentLength + 1

        // Finish the last/current segment, then return the list of segments as an array.
        let offset = len1 - segmentLength

        System.ArraySegment<_> (array1, offset, segmentLength)
        |> segments1.Add
        System.ArraySegment<_> (array2, offset, segmentLength)
        |> segments2.Add

        segments1.ToArray(), segments2.ToArray()

    /// Expands an array by creating a copy of it which has
    /// the specified number of empty elements appended to it.
    [<CompiledName("ExpandRight")>]
    let expandRight count (arr : 'T[]) =
        // Preconditions
        if count < 0 then
            invalidArg "count" "The number of elements to expand the array by is negative."

        // Create the new "expanded" array. Copy the elements from the original array
        // into the "left" side of the new array, then return the expanded array.
        let expandedArr = Array.zeroCreate (arr.Length + count)        
        Array.blit arr 0 expandedArr 0 arr.Length
        expandedArr

    /// Expands an array by creating a copy of it which has
    /// the specified number of empty elements prepended to it.
    [<CompiledName("ExpandLeft")>]
    let expandLeft count (arr : 'T[]) =
        // Preconditions
        if count < 0 then
            invalidArg "count" "The number of elements to expand the array by is negative."

        // Create the new "expanded" array. Copy the elements from the original array
        // into the "right" side of the new array, then return the expanded array.
        let expandedArr = Array.zeroCreate (arr.Length + count)
        Array.blit arr 0 expandedArr count arr.Length
        expandedArr

    // TODO : foldPairs, foldBackPairs
    // TODO : derive    // takes a 'T -> 'T -> 'T like reduce, but only performs one step; used to perform 'divided differences'


//
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TaggedArray =
    /// Similar to Array.mapi, but 'tags' the index values with a unit-of-measure
    /// type before applying them to the mapping function.
    [<CompiledName("MapIndexedByTag")>]
    let mapti (mapping : int<'Tag> -> 'T -> 'U) (array : 'T[]) =
        checkNonNull "array" array
        let mapping = FSharpFunc<_,_,_>.Adapt mapping
        let len = array.Length
        let results = Array.zeroCreate len
        for i = 0 to len - 1 do
            results.[i] <- mapping.Invoke (Int32WithMeasure<'Tag> i, array.[i])
        results

    /// Similar to Array.mapi, but 'tags' the index values with a unit-of-measure
    /// type before applying them to the mapping function.
    [<CompiledName("MapIndexedByTag")>]
    let mapti2 (mapping : int<'Tag> -> 'T1 -> 'T2 -> 'U) (array1 : 'T1[]) (array2 : 'T2[]) =
        checkNonNull "array1" array1
        checkNonNull "array2" array2
        let mapping = FSharpFunc<_,_,_,_>.Adapt mapping
        let len1 = array1.Length 
        if len1 <> array2.Length then
            invalidArg "array2" "The arrays have differing lengths."

        let results = Array.zeroCreate len1
        for i = 0 to len1 - 1 do
            results.[i] <- mapping.Invoke (Int32WithMeasure<'Tag> i, array1.[i], array2.[i])
        results

    /// Applies a function to each element of the collection, threading an accumulator argument through the computation.
    /// The integer index passed to the function indicates the array index of the element being transformed.
    /// The index values are tagged with a unit-of-measure type before applying them to the folder function.
    [<CompiledName("FoldIndexedByTag")>]
    let foldti (folder : int<'Tag> -> 'State -> 'T -> 'State) (state : 'State) (arr : 'T[]) =
        checkNonNull "arr" arr
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt folder
        let mutable state = state
        let len = arr.Length
        for i = 0 to len - 1 do
            state <- folder.Invoke (Int32WithMeasure<'Tag> i, state, arr.[i])
        state


/// Functional operators on ArraySegments.
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ArraySegment =
    (* OPTIMIZE :   Get rid of the recursive implementation functions below and
                    re-implement functions using imperative loops. This simplifies
                    the code and should also be slightly faster. *)

    //
    let [<NoDynamicInvocation>] inline array (segment : ArraySegment<'T>) =
        segment.Array

    //
    let [<NoDynamicInvocation>] inline count (segment : ArraySegment<'T>) =
        segment.Count

    //
    let [<NoDynamicInvocation>] inline offset (segment : ArraySegment<'T>) =
        segment.Offset

    //
    let [<NoDynamicInvocation>] inline isEmpty (arrSeg : ArraySegment<'T>) =
        arrSeg.Count = 0

    /// Creates an ArraySegment spanning the entire length of an array.
    let [<NoDynamicInvocation>] inline ofArray (array : 'T[]) : ArraySegment<'T> =
        ArraySegment (array)

    /// Gets an element of an ArraySegment<'T>.
    [<CompiledName("Get")>]
    let get (segment : ArraySegment<'T>) index =
        if index < 0 || index >= (count segment) then
            raise <| System.IndexOutOfRangeException ()
        else
            segment.Array.[segment.Offset + index]

    /// Sets an element of an ArraySegment<'T>.
    [<CompiledName("Set")>]
    let set (segment : ArraySegment<'T>) index value =
        if index < 0 || index >= (count segment) then
            raise <| System.IndexOutOfRangeException ()
        else
            segment.Array.[segment.Offset + index] <- value

    /// Gets the first element in an ArraySegment<'T>.
    let [<NoDynamicInvocation>] inline first (segment : ArraySegment<'T>) =
        if isEmpty segment then
            invalidOp "Cannot retrieve the first element of an empty ArraySegment<'T>."
        else segment.Array.[segment.Offset]

    /// Gets the index of the last element in an ArraySegment<'T>, within the original array.
    /// NOTE : This implemention is meant for internal use only, and does NOT perform bounds checking.
    let [<NoDynamicInvocation>] inline private lastIndexUnsafe (segment : ArraySegment<'T>) =
        segment.Offset + (segment.Count - 1)

    /// Gets the index of the last element in an ArraySegment<'T>, within the original array.
    let [<NoDynamicInvocation>] inline lastIndex (segment : ArraySegment<'T>) =
        if isEmpty segment then
            invalidOp "The ArraySegment<'T> is empty."
        else lastIndexUnsafe segment

    /// Gets the last element in an ArraySegment<'T>.
    let [<NoDynamicInvocation>] inline last (segment : ArraySegment<'T>) =
        if isEmpty segment then
            invalidOp "Cannot retrieve the last element of an empty ArraySegment<'T>."
        else segment.Array.[lastIndexUnsafe segment]

    /// Builds a new array from the elements within the ArraySegment<'T>.
    [<CompiledName("ToArray")>]
    let toArray (segment : ArraySegment<'T>) =
        if isEmpty segment then
            Array.empty
        else
            segment.Array.[segment.Offset .. (lastIndexUnsafe segment)]

    //
    [<CompiledName("MapToArray")>]
    let mapToArray (mapping : 'T -> 'U) (segment : ArraySegment<'T>) =
        if isEmpty segment then
            Array.empty
        else
            //
            let lastIndex = lastIndexUnsafe segment
            //
            let results = Array.zeroCreate (count segment)

            //
            let arr = array segment
            for i = (offset segment) to lastIndex do
                results.[i] <- mapping arr.[i]

            // Return the mapped results.
            results

    //
    [<CompiledName("TryPick")>]
    let tryPick picker (segment : ArraySegment<'T>) : 'U option =
        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        let mutable matchResult = None
        let mutable index = segment.Offset

        while Option.isNone matchResult && index <= endIndex do
            match picker array.[index] with
            | Some result ->
                matchResult <- result
            | None ->
                index <- index + 1

        // Return the result (if a match was found) or None.
        matchResult

    //
    [<CompiledName("Pick")>]
    let pick picker (segment : ArraySegment<'T>) : 'U =
        // Call tryPick to find the value; if no match is found, raise an exception.
        match tryPick picker segment with
        | Some result ->
            result
        | None ->
            raise <| System.Collections.Generic.KeyNotFoundException ()

    //
    [<CompiledName("TryFind")>]
    let tryFind predicate (segment : ArraySegment<'T>) =
        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        let mutable matchedElement = None
        let mutable index = segment.Offset

        while Option.isNone matchedElement && index <= endIndex do
            let el = array.[index]
            if predicate el then
                matchedElement <- Some el
            else
                index <- index + 1

        // Return the result (if a match was found) or None.
        matchedElement

    //
    [<CompiledName("Find")>]
    let find predicate (segment : ArraySegment<'T>) =
        // Call tryFind to find the value; if no match is found, raise an exception.
        match tryFind predicate segment with
        | Some element ->
            element
        | None ->
            raise <| System.Collections.Generic.KeyNotFoundException ()

    //
    [<CompiledName("TryFindIndex")>]
    let tryFindIndex predicate (segment : ArraySegment<'T>) =
        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        let mutable matchedIndex = None
        let mutable index = segment.Offset

        while Option.isNone matchedIndex && index <= endIndex do
            if predicate array.[index] then
                matchedIndex <- Some index
            else
                index <- index + 1

        // Return the result (if a match was found) or None.
        matchedIndex

    //
    [<CompiledName("FindIndex")>]
    let findIndex predicate (segment : ArraySegment<'T>) =
        // Call tryFindIndex to find the value; if no match is found, raise an exception.
        match tryFindIndex predicate segment with
        | Some index ->
            index
        | None ->
            raise <| System.Collections.Generic.KeyNotFoundException ()

    //
    [<CompiledName("Iterate")>]
    let iter action (segment : ArraySegment<'T>) =
        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        for i = segment.Offset to endIndex do
            action array.[i]

    //
    [<CompiledName("Exists")>]
    let exists predicate (segment : ArraySegment<'T>) =
        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        let mutable foundMatchingElement = false
        let mutable index = segment.Offset

        while not foundMatchingElement && index <= endIndex do
            if predicate array.[index] then
                foundMatchingElement <- true
            else
                index <- index + 1

        // Return the value indicating if any element matched the predicate.
        foundMatchingElement

    //
    [<CompiledName("Forall")>]
    let forall predicate (segment : ArraySegment<'T>) =
        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        let mutable allElementsMatched = true
        let mutable index = segment.Offset

        while allElementsMatched && index <= endIndex do
            if predicate array.[index] then
                index <- index + 1
            else
                allElementsMatched <- false

        // Return the value indicating if all elements matched the predicate.
        allElementsMatched

    //
    [<CompiledName("Fold")>]
    let fold folder (state : 'State) (segment : ArraySegment<'T>) =
        let folder = FSharpFunc<_,_,_>.Adapt folder

        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        let mutable state = state
        for i = segment.Offset to endIndex do
            state <- folder.Invoke (state, array.[i])

        // Return the final state value.
        state

    //
    [<CompiledName("FoldBack")>]
    let foldBack folder (segment : ArraySegment<'T>) (state : 'State) =
        let folder = FSharpFunc<_,_,_>.Adapt folder

        // OPTIMIZATION : Use imperative/mutable style for maximum performance.
        let array = segment.Array
        /// The last index (inclusive) in the underlying array which belongs to this ArraySegment.
        let endIndex = lastIndexUnsafe segment

        let mutable state = state
        for i = endIndex downto segment.Offset do
            state <- folder.Invoke (array.[i], state)

        // Return the final state value.
        state

    //
    [<CompiledName("Reduce")>]
    let reduce (reduction : 'T -> 'T -> 'T) (segment : ArraySegment<'T>) =
        // Preconditions
        if isEmpty segment then
            invalidArg "segment" "Cannot reduce an empty ArraySegment<'T>."

        // Create a new array segment which excludes the first element
        // of the input segment, then call 'fold' with it.
        let segment' = ArraySegment (segment.Array, segment.Offset + 1, segment.Count - 1)
        fold reduction segment.[0] segment'

    //
    [<CompiledName("ReduceBack")>]
    let reduceBack (reduction : 'T -> 'T -> 'T) (segment : ArraySegment<'T>) =
        // Preconditions
        if isEmpty segment then
            invalidArg "segment" "Cannot reduce an empty ArraySegment<'T>."

        // Create a new array segment which excludes the last element
        // of the input segment, then call 'foldBack' with it.
        let segment' = ArraySegment (segment.Array, segment.Offset, segment.Count - 1)
        foldBack reduction segment' segment.[segment.Count - 1]

    //
    [<CompiledName("ToList")>]
    let toList (segment : ArraySegment<'T>) =
        // OPTIMIZATION : If the segment is empty return immediately.
        if isEmpty segment then []
        else
            // Fold backwards so we don't need to reverse the list
            // we create -- it'll already be in the correct order.
            (segment, [])
            ||> foldBack (fun el list ->
                el :: list)

    //
    [<CompiledName("Minimum")>]
    let min<'T when 'T : comparison> (segment : ArraySegment<'T>) =
        // Preconditions
        if isEmpty segment then
            invalidArg "segment" "Cannot compute the minimum element of an empty ArraySegment<'T>."

        reduce min segment

    //
    [<CompiledName("Maximum")>]
    let max<'T when 'T : comparison> (segment : ArraySegment<'T>) =
        // Preconditions
        if isEmpty segment then
            invalidArg "segment" "Cannot compute the maximum element of an empty ArraySegment<'T>."

        reduce max segment

    //
    [<CompiledName("Sum")>]
    let inline sum (segment : ArraySegment<'T>) =
        // Preconditions
        if isEmpty segment then
            invalidArg "segment" "Cannot compute the sum of an empty ArraySegment<'T>."

        reduce (+) segment

    // TODO
    // minBy
    // maxBy    
    // sumBy   


/// Additional functional operators on sets.
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Set =
    /// Returns the number of elements in the set as an unsigned integer.
    let [<NoDynamicInvocation>] inline natCount s =
        uint32 <| Set.count s

    //
    [<CompiledName("OfArraySegment")>]
    let ofArraySegment (segment : System.ArraySegment<'T>) : Set<'T> =
        (Set.empty, segment)
        ||> ArraySegment.fold (fun set el ->
            Set.add el set)

    //
    [<CompiledName("FoldIndexed")>]
    let foldi (folder : int -> 'State -> 'T -> 'State) (state : 'State) (set : Set<'T>) =
        checkNonNull "set" set
        let folder = FSharpFunc<_,_,_,_>.Adapt folder
        let mutable state = state
        let mutable idx = 0
        for el in set do
            state <- folder.Invoke (idx, state, el)
            idx <- idx + 1
        state

    //
    [<CompiledName("MapToArray")>]
    let mapToArray (mapping : 'T -> 'U) (set : Set<'T>) =
        checkNonNull "set" set
        let results = Array.zeroCreate <| Set.count set
        let mutable idx = 0
        for el in set do
            results.[idx] <- mapping el
            idx <- idx + 1
        results

    /// Creates a set given a number of items in the set and a generator function.
    [<CompiledName("Initialize")>]
    let init count (initializer : int -> 'T) =
        // Preconditions
        if count < 0 then
            invalidArg "count" "The number of items cannot be negative."

        // Optimize the empty set case.
        if count = 0 then
            Set.empty
        else
            let mutable result = Set.empty
            for i = 0 to (count - 1) do
                result <- Set.add (initializer i) result
            result

    /// Reduces elements in a set in order from the least (minimum) element
    /// to the greatest (maximum) element.
    [<CompiledName("Reduce")>]
    let reduce (reduction : 'T -> 'T -> 'T) (set : Set<'T>) =
        // Preconditions
        checkNonNull "set" set
        if Set.isEmpty set then
            invalidArg "set" "The set is empty."

        let minElement = Set.minElement set
        let set = Set.remove minElement set
        Set.fold reduction minElement set

    /// Reduces elements in a set in order from the greatest (maximum) element
    /// to the least (minimum) element.
    [<CompiledName("ReduceBack")>]
    let reduceBack (reduction : 'T -> 'T -> 'T) (set : Set<'T>) =
        // Preconditions
        checkNonNull "set" set
        if Set.isEmpty set then
            invalidArg "set" "The set is empty."

        let maxElement = Set.maxElement set
        let set = Set.remove maxElement set
        Set.foldBack reduction set maxElement

    //
    [<CompiledName("Choose")>]
    let choose (chooser : 'T -> 'U option) (set : Set<'T>) : Set<'U> =
        // Preconditions
        checkNonNull "set" set

        // OPTIMIZATION : If the input set is empty return immediately.
        if Set.isEmpty set then
            Set.empty
        else
            (Set.empty, set)
            ||> Set.fold (fun chosen el ->
                match chooser el with
                | None ->
                    chosen
                | Some result ->
                    Set.add result chosen)

    //
    [<CompiledName("TryPick")>]
    let tryPick (picker : 'T -> 'U option) (set : Set<'T>) : 'U option =
        // Preconditions
        checkNonNull "set" set

        // OPTIMIZATION : If the input set is empty return immediately.
        if Set.isEmpty set then None
        else
            // Use an iterator over the set here since we don't
            // have access to the internal structure of F# Sets.
            Set.toSeq set
            |> Seq.tryPick picker

    //
    [<CompiledName("Pick")>]
    let pick (picker : 'T -> 'U option) (set : Set<'T>) : 'U =
        // Preconditions
        checkNonNull "set" set

        // Use tryPick to find a matching element and
        // raise an exception if it can't.
        match tryPick picker set with
        | Some result ->
            result
        | None ->
            raise <| System.Collections.Generic.KeyNotFoundException ()

    //
    [<CompiledName("TryFind")>]
    let tryFind (predicate : 'T -> bool) (set : Set<'T>) : 'T option =
        // Preconditions
        checkNonNull "set" set

        // OPTIMIZATION : If the input set is empty return immediately.
        if Set.isEmpty set then None
        else
            // Use an iterator over the set here since we don't
            // have access to the internal structure of F# Sets.
            Set.toSeq set
            |> Seq.tryFind predicate

    //
    [<CompiledName("Find")>]
    let find (predicate : 'T -> bool) (set : Set<'T>) : 'T =
        // Preconditions
        checkNonNull "set" set

        // Use tryFind to find a matching element and
        // raise an exception if it can't.
        match tryFind predicate set with
        | Some result ->
            result
        | None ->
            raise <| System.Collections.Generic.KeyNotFoundException ()


/// Additional functional operators on maps.
[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Map =
    /// Determines the number of items in the Map.
    let [<NoDynamicInvocation>] inline count (map : Map<'Key, 'Value>) =
        map.Count

    /// Combines two maps into a single map.
    /// Whenever a key exists in both maps, the first map's entry will be added to the result map.
    let union (map1 : Map<'Key, 'T>) (map2 : Map<'Key, 'T>) : Map<'Key, 'T> =
        // Preconditions
        checkNonNull "map1" map1
        checkNonNull "map2" map2

        match map1.Count, map2.Count with
        // Optimize for empty inputs
        | 0, 0 ->
            Map.empty
        | 0, _ ->
            map2
        | _, 0 ->
            map1
        | _, _ ->
            // Start with the second map.
            // Fold over the first map, adding it's entries to the second
            // and overwriting any existing entries.
            (map2, map1)
            ||> Map.fold (fun combinedMap key value ->
                Map.add key value combinedMap)

