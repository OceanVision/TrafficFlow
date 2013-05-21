using System;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Collections;
using MicrosoftResearch.Infer.Factors;
using System.Collections.Generic;

namespace MicrosoftResearch.Infer.Models.User
{
	/// <summary>
	/// Generated algorithm for performing inference
	/// </summary>
	/// <remarks>
	/// The easiest way to use this class is to wrap an instance in a CompiledAlgorithm object and use
	/// the methods on CompiledAlgorithm to set parameters and execute inference.
	/// 
	/// If you instead wish to use this class directly, you must perform the following steps:
	/// 1) Create an instance of the class
	/// 2) Set the value of any externally-set fields e.g. data, priors
	/// 3) Call the Execute(numberOfIterations) method
	/// 4) Use the XXXMarginal() methods to retrieve posterior marginals for different variables.
	/// 
	/// Generated by Infer.NET 2.5 at 04:18 on 21 maja 2013.
	/// </remarks>
	public partial class Model_Gibbs : IGeneratedAlgorithm
	{
		#region Fields
		/// <summary>Field backing the NumberOfIterationsDone property</summary>
		private int numberOfIterationsDone;
		/// <summary>Field backing the vVector0 property</summary>
		private Vector VVector0;
		/// <summary>The number of iterations last computed by Changed_vVector0. Set this to zero to force re-execution of Changed_vVector0</summary>
		public int Changed_vVector0_iterationsDone;
		/// <summary>The number of iterations last computed by Changed_vVector0_Init_numberOfIterationsDecreased. Set this to zero to force re-execution of Changed_vVector0_Init_numberOfIterationsDecreased</summary>
		public int Changed_vVector0_Init_numberOfIterationsDecreased_iterationsDone;
		/// <summary>True if Changed_vVector0_Init_numberOfIterationsDecreased has performed initialisation. Set this to false to force re-execution of Changed_vVector0_Init_numberOfIterationsDecreased</summary>
		public bool Changed_vVector0_Init_numberOfIterationsDecreased_isInitialised;
		/// <summary>The number of iterations last computed by Changed_numberOfIterationsDecreased_vVector0. Set this to zero to force re-execution of Changed_numberOfIterationsDecreased_vVector0</summary>
		public int Changed_numberOfIterationsDecreased_vVector0_iterationsDone;
		/// <summary>Message to marginal of 'vint1'</summary>
		public GibbsMarginal<Discrete,int> vint1_marginal_F;
		public PointMass<Vector> vVector0_marginal;
		#endregion

		#region Properties
		/// <summary>The number of iterations done from the initial state</summary>
		public int NumberOfIterationsDone
		{			get {
				return this.numberOfIterationsDone;
			}
		}

		/// <summary>The externally-specified value of 'vVector0'</summary>
		public Vector vVector0
		{			get {
				return this.VVector0;
			}
			set {
				this.VVector0 = value;
				this.numberOfIterationsDone = 0;
				this.Changed_vVector0_Init_numberOfIterationsDecreased_iterationsDone = 0;
				this.Changed_numberOfIterationsDecreased_vVector0_iterationsDone = 0;
				this.Changed_vVector0_iterationsDone = 0;
			}
		}

		#endregion

		#region Methods
		/// <summary>Get the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object GetObservedValue(string variableName)
		{
			if (variableName=="vVector0") {
				return this.vVector0;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>Set the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		/// <param name="value">Observed value</param>
		public void SetObservedValue(string variableName, object value)
		{
			if (variableName=="vVector0") {
				this.vVector0 = (Vector)value;
				return ;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>The marginal distribution of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object Marginal(string variableName)
		{
			if (variableName=="vint1") {
				return this.Vint1Marginal();
			}
			if (variableName=="vVector0") {
				return this.VVector0Marginal();
			}
			throw new ArgumentException("This class was not built to infer "+variableName);
		}

		public T Marginal<T>(string variableName)
		{
			return Distribution.ChangeType<T>(this.Marginal(variableName));
		}

		/// <summary>The query-specific marginal distribution of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		/// <param name="query">QueryType name. For example, GibbsSampling answers 'Marginal', 'Samples', and 'Conditionals' queries</param>
		public object Marginal(string variableName, string query)
		{
			if (query=="Marginal") {
				return this.Marginal(variableName);
			}
			if ((variableName=="vint1")&&(query=="Samples")) {
				return this.Vint1Samples();
			}
			throw new ArgumentException(((("This class was not built to infer \'"+variableName)+"\' with query \'")+query)+"\'");
		}

		public T Marginal<T>(string variableName, string query)
		{
			return Distribution.ChangeType<T>(this.Marginal(variableName, query));
		}

		/// <summary>Update all marginals, by iterating message passing the given number of times</summary>
		/// <param name="numberOfIterations">The number of times to iterate each loop</param>
		/// <param name="initialise">If true, messages that initialise loops are reset when observed values change</param>
		private void Execute(int numberOfIterations, bool initialise)
		{
			if (numberOfIterations<this.Changed_numberOfIterationsDecreased_vVector0_iterationsDone) {
				this.Changed_vVector0_Init_numberOfIterationsDecreased_isInitialised = false;
				this.Changed_numberOfIterationsDecreased_vVector0_iterationsDone = 0;
			}
			this.Changed_vVector0_Init_numberOfIterationsDecreased(initialise);
			this.Changed_numberOfIterationsDecreased_vVector0(numberOfIterations);
			this.Changed_vVector0();
			this.numberOfIterationsDone = numberOfIterations;
		}

		public void Execute(int numberOfIterations)
		{
			this.Execute(numberOfIterations, true);
		}

		public void Update(int additionalIterations)
		{
			this.Execute(this.numberOfIterationsDone+additionalIterations, false);
		}

		private void OnProgressChanged(ProgressChangedEventArgs e)
		{
			// Make a temporary copy of the event to avoid a race condition
			// if the last subscriber unsubscribes immediately after the null check and before the event is raised.
			EventHandler<ProgressChangedEventArgs> handler = this.ProgressChanged;
			if (handler!=null) {
				handler(this, e);
			}
		}

		/// <summary>Reset all messages to their initial values.  Sets NumberOfIterationsDone to 0.</summary>
		public void Reset()
		{
			this.Execute(0);
		}

		/// <summary>Computations that depend on the observed value of vVector0 and must reset on changes to numberOfIterationsDecreased</summary>
		/// <param name="initialise">If true, reset messages that initialise loops</param>
		public void Changed_vVector0_Init_numberOfIterationsDecreased(bool initialise)
		{
			if ((this.Changed_vVector0_Init_numberOfIterationsDecreased_iterationsDone==1)&&((!initialise)||this.Changed_vVector0_Init_numberOfIterationsDecreased_isInitialised)) {
				return ;
			}
			this.vint1_marginal_F = new GibbsMarginal<Discrete,int>(Discrete.Uniform(this.VVector0.Count), 100, 5, true, true, false);
			this.Changed_vVector0_Init_numberOfIterationsDecreased_iterationsDone = 1;
			this.Changed_vVector0_Init_numberOfIterationsDecreased_isInitialised = true;
			this.Changed_numberOfIterationsDecreased_vVector0_iterationsDone = 0;
		}

		/// <summary>Computations that depend on the observed value of numberOfIterationsDecreased and vVector0</summary>
		/// <param name="numberOfIterations">The number of times to iterate each loop</param>
		public void Changed_numberOfIterationsDecreased_vVector0(int numberOfIterations)
		{
			if (this.Changed_numberOfIterationsDecreased_vVector0_iterationsDone==numberOfIterations) {
				return ;
			}
			// Messages from uses of 'vint1'
			Discrete[] vint1_uses_B = default(Discrete[]);
			// Create array for 'vint1_uses' Backwards messages.
			vint1_uses_B = new Discrete[0];
			// Message from definition of 'vint1'
			Discrete vint1_F = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.VVector0.Count));
			// Message to 'vint1' from Discrete factor
			vint1_F = DiscreteFromDirichletOp.SampleAverageConditional(this.VVector0, vint1_F);
			for(int iteration = this.Changed_numberOfIterationsDecreased_vVector0_iterationsDone; iteration<numberOfIterations; iteration++) {
				// Message to 'vint1_marginal' from UsesEqualDef factor
				this.vint1_marginal_F = UsesEqualDefGibbsOp<int>.MarginalGibbs<Discrete>(vint1_uses_B, vint1_F, this.vint1_marginal_F);
				this.OnProgressChanged(new ProgressChangedEventArgs(iteration));
			}
			// Message to 'vint1_marginal' from UsesEqualDef factor
			this.vint1_marginal_F = UsesEqualDefGibbsOp<int>.MarginalGibbs<Discrete>(vint1_uses_B, vint1_F, this.vint1_marginal_F);
			this.Changed_numberOfIterationsDecreased_vVector0_iterationsDone = numberOfIterations;
		}

		/// <summary>
		/// Returns the Samples for 'vint1' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The Samples</returns>
		public IList<int> Vint1Samples()
		{
			return this.vint1_marginal_F.Samples;
		}

		/// <summary>
		/// Returns the Marginal for 'vint1' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The Marginal</returns>
		public Discrete Vint1Marginal()
		{
			return this.vint1_marginal_F.Distribution;
		}

		/// <summary>Computations that depend on the observed value of vVector0</summary>
		public void Changed_vVector0()
		{
			if (this.Changed_vVector0_iterationsDone==1) {
				return ;
			}
			this.vVector0_marginal = new PointMass<Vector>(this.VVector0);
			this.Changed_vVector0_iterationsDone = 1;
		}

		/// <summary>
		/// Returns the marginal distribution for 'vVector0' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public PointMass<Vector> VVector0Marginal()
		{
			return this.vVector0_marginal;
		}

		#endregion

		#region Events
		/// <summary>Event that is fired when the progress of inference changes, typically at the end of one iteration of the inference algorithm.</summary>
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
		#endregion

	}

}
